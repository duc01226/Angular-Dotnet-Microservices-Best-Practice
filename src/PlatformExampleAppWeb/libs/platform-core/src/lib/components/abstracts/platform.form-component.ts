/* eslint-disable @typescript-eslint/no-explicit-any */
import { Directive, Input, OnInit, QueryList } from '@angular/core';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { filter } from 'rxjs';
import { ArrayElement } from 'type-fest/source/exact';

import { IPlatformFormValidationError } from '../../form-validators';
import { FormHelpers } from '../../helpers';
import { immutableUpdate, isDifferent, keys } from '../../utils';
import { IPlatformVm, PlatformFormMode } from '../../view-models';
import { PlatformVmComponent } from './platform.vm-component';

export interface IPlatformFormComponent {
  isFormValid(): boolean;

  isAllChildFormsValid(forms: QueryList<IPlatformFormComponent>[]): boolean;

  validateForm(): boolean;

  validateAllChildForms(forms: QueryList<IPlatformFormComponent>[]): boolean;

  formControls(key: string): FormControl;

  formControlsError(controlKey: string, errorKey: string): IPlatformFormValidationError | null;
}

@Directive()
export abstract class PlatformFormComponent<TViewModel extends IPlatformVm>
  extends PlatformVmComponent<TViewModel>
  implements IPlatformFormComponent, OnInit
{
  public constructor() {
    super();
  }

  protected _mode: PlatformFormMode = 'create';
  public get mode(): PlatformFormMode {
    return this._mode;
  }
  @Input()
  public set mode(v: PlatformFormMode) {
    const prevMode = this._mode;
    this._mode = v;

    if (this.initiated$.value) {
      if (prevMode == 'view' && (v == 'create' || v == 'update')) {
        this.form.enable();
        this.patchVmValuesToForm(this.vm);
        this.validateForm();
      }
    }
  }

  @Input() public form!: FormGroup<PlatformFormGroupControls<TViewModel>>;
  @Input() public formConfig!: PlatformFormConfig<TViewModel>;

  public get isViewMode(): boolean {
    return this.mode === 'view';
  }

  public get isCreateMode(): boolean {
    return this.mode === 'create';
  }

  public get isUpdateMode(): boolean {
    return this.mode === 'update';
  }

  protected abstract initialFormConfig: () => PlatformFormConfig<TViewModel> | undefined;

  public override ngOnInit(): void {
    super.ngOnInit();

    // If form and formConfig has NOT been given via input
    if (!this.formConfig && !this.form) {
      if (this.initiated$.value == true) {
        this.initForm();
      } else {
        // Init empty form
        this.form = new FormGroup<PlatformFormGroupControls<TViewModel>>(<any>{});

        this.storeAnonymousSubscription(
          this.initiated$.pipe(filter(initiated => initiated)).subscribe(() => {
            this.initForm();
          })
        );
      }
    }
  }

  protected initForm() {
    if (this.formConfig && this.form) return;

    const initialFormConfig = this.initialFormConfig();
    if (initialFormConfig == undefined)
      throw new Error('initialFormConfig must not be undefined or formConfig and form must be input');

    this.formConfig = initialFormConfig;
    this.form = this.buildForm(this.formConfig);

    keys(this.form.controls).forEach(formControlKey => {
      this.storeAnonymousSubscription(
        (<FormControl>(<any>this.form.controls)[formControlKey]).valueChanges.subscribe(value => {
          this.updateVmOnFormValuesChange(<Partial<TViewModel>>{ [formControlKey]: value });
          this.processGroupValidation(formControlKey);
        })
      );
    });

    this.patchVmValuesToForm(this.vm);

    if (!this.isViewMode) this.validateForm();
    else this.form.disable();

    if (this.formConfig.afterInit) this.formConfig.afterInit();
  }

  protected override internalSetVm = (v: TViewModel, shallowCheckDiff: boolean = true): void => {
    if (shallowCheckDiff == false || this._vm != v) {
      this._vm = v;

      if (this.initiated$.value) {
        this.patchVmValuesToForm(v);
        this.detectChanges();
        this.vmChangeEvent.emit(v);
      }
    }
  };

  public isFormValid(): boolean {
    return (
      this.form.valid &&
      (this.formConfig.childForms == undefined || this.isAllChildFormsValid(this.formConfig.childForms()))
    );
  }

  public isAllChildFormsValid(forms: (QueryList<IPlatformFormComponent> | IPlatformFormComponent)[]): boolean {
    const invalidChildFormsGroup = forms.find(childFormOrFormsGroup =>
      childFormOrFormsGroup instanceof QueryList
        ? childFormOrFormsGroup.find(formComponent => !formComponent.isFormValid()) != undefined
        : !childFormOrFormsGroup.isFormValid()
    );

    return invalidChildFormsGroup == undefined;
  }

  public validateForm(): boolean {
    return (
      FormHelpers.validateForm(this.form) &&
      (this.formConfig.childForms == undefined || this.validateAllChildForms(this.formConfig.childForms()))
    );
  }

  public validateAllChildForms(forms: (QueryList<IPlatformFormComponent> | IPlatformFormComponent)[]): boolean {
    const invalidChildFormsGroup = forms.find(childFormOrFormsGroup =>
      childFormOrFormsGroup instanceof QueryList
        ? childFormOrFormsGroup.find(formComponent => !formComponent.validateForm()) != undefined
        : !childFormOrFormsGroup.validateForm()
    );

    return invalidChildFormsGroup == undefined;
  }

  public patchVmValuesToForm(vm: TViewModel): void {
    const vmFormValues: Partial<TViewModel> = this.getFromVmFormValues(vm);
    const currentReactiveFormValues: Partial<TViewModel> = this.getCurrentReactiveFormControlValues();

    keys(vmFormValues).forEach(formKey => {
      const vmFormKeyValue = (<any>vmFormValues)[formKey];
      const formControl = (<any>this.form.controls)[formKey];

      if (isDifferent(vmFormKeyValue, (<any>currentReactiveFormValues)[formKey])) {
        if (
          formControl instanceof FormArray &&
          vmFormKeyValue instanceof Array &&
          formControl.length != vmFormKeyValue.length
        ) {
          formControl.clear({ emitEvent: false });
          vmFormKeyValue.forEach((modelItem, index) =>
            formControl.push(
              this.buildFromArrayControlItem((<any>this.formConfig.controls)[formKey], modelItem, index),
              {
                emitEvent: false
              }
            )
          );
        }

        this.form.patchValue(<any>{ [formKey]: vmFormKeyValue }, { emitEvent: false });

        if (!this.isViewMode) this.processGroupValidation(formKey);
      }
    });
  }

  public getCurrentReactiveFormControlValues(): Partial<TViewModel> {
    const reactiveFormValues: Partial<TViewModel> = {};

    keys(this.formConfig.controls).forEach(formControlKey => {
      (<any>reactiveFormValues)[formControlKey] = (<any>this.form.controls)[formControlKey].value;
    });

    return reactiveFormValues;
  }

  public getFromVmFormValues(vm: TViewModel): Partial<TViewModel> {
    const vmFormValues: Partial<TViewModel> = {};

    keys(this.formConfig.controls).forEach(formControlKey => {
      (<any>vmFormValues)[formControlKey] = (<any>vm)[formControlKey];
    });

    return vmFormValues;
  }

  public formControls(key: string): FormControl {
    return <FormControl>this.form.get(key);
  }

  public formControlsError(controlKey: string, errorKey: string): IPlatformFormValidationError | null {
    return this.formControls(controlKey)?.errors?.[errorKey];
  }

  public processGroupValidation(formControlKey: keyof TViewModel | string | number | symbol) {
    if (this.formConfig.groupValidations == null) return;

    this.formConfig.groupValidations.forEach(groupValidators => {
      if (groupValidators.includes(<keyof TViewModel>formControlKey))
        groupValidators.forEach(groupValidatorControlKey => {
          this.formControls(groupValidatorControlKey.toString()).updateValueAndValidity({
            emitEvent: false,
            onlySelf: true
          });
        });
    });
  }

  protected formGroupArrayFor<TItemModel>(
    items: TItemModel[],
    formItemGroupControls: (item: TItemModel) => PlatformPartialFormGroupControls<TItemModel>
  ): FormArray<FormGroup<PlatformFormGroupControls<TItemModel>>> {
    return new FormArray(
      items.map(item => new FormGroup(<PlatformFormGroupControls<TItemModel>>formItemGroupControls(item)))
    );
  }

  protected formControlArrayFor<TItemModel>(
    items: TItemModel[],
    formItemControl: (item: TItemModel) => FormControl<TItemModel>
  ): FormArray<FormControl<TItemModel>> {
    return new FormArray(items.map(item => formItemControl(item)));
  }

  protected updateVmOnFormValuesChange(values: Partial<TViewModel>) {
    const newUpdatedVm: TViewModel = immutableUpdate(this.vm, values, true);

    if (newUpdatedVm != this.vm) {
      this.internalSetVm(newUpdatedVm, false);
    }
  }

  protected buildForm(formConfig: PlatformFormConfig<TViewModel>): FormGroup<PlatformFormGroupControls<TViewModel>> {
    const controls = <PlatformFormGroupControls<TViewModel>>{};

    keys(formConfig.controls).forEach(key => {
      const formConfigControlsConfigItem: PlatformFormGroupControlConfigProp<unknown> = (<any>formConfig.controls)[key];
      const formConfigControlsConfigArrayItem = <PlatformFormGroupControlConfigPropArray<unknown>>(
        (<any>formConfig.controls)[key]
      );

      if (formConfigControlsConfigItem instanceof FormControl) {
        (<any>controls)[key] = formConfigControlsConfigItem;
      } else if (
        formConfigControlsConfigArrayItem.itemControl != undefined &&
        formConfigControlsConfigArrayItem.modelItems != undefined
      ) {
        (<any>controls)[key] = new FormArray(
          formConfigControlsConfigArrayItem.modelItems().map((modelItem, index) => {
            return this.buildFromArrayControlItem(formConfigControlsConfigArrayItem, modelItem, index);
          })
        );
      }
    });

    return new FormGroup(controls);
  }

  protected buildFromArrayControlItem(
    formConfigControlsConfigArrayItem: PlatformFormGroupControlConfigPropArray<unknown>,
    modelItem: unknown,
    modelItemIndex: number
  ) {
    const itemControl = formConfigControlsConfigArrayItem.itemControl(modelItem, modelItemIndex);
    return itemControl instanceof FormControl ? itemControl : new FormGroup(itemControl);
  }
}

export type PlatformFormConfig<TFormModel> = {
  controls: PlatformPartialFormGroupControlsConfig<TFormModel>;
  groupValidations?: (keyof TFormModel)[][];
  afterInit?: () => void;
  childForms?: () => (QueryList<IPlatformFormComponent> | IPlatformFormComponent)[];
};

export type PlatformPartialFormGroupControlsConfig<TFormModel> = {
  [P in keyof TFormModel]?: TFormModel[P] extends readonly unknown[]
    ? FormControl<TFormModel[P]> | PlatformFormGroupControlConfigPropArray<ArrayElement<TFormModel[P]>>
    : FormControl<TFormModel[P]>;
};

// Need to be code duplicated used in "export type PlatformPartialFormGroupControlsConfig<TFormModel> = {"
// "[P in keyof TFormModel]?: TFormModel[P] ..." should be equal to PlatformFormGroupControlConfigProp<TFormModel[P]>
// dont know why it will get type errors when using if TFormModel[P] is enum
export type PlatformFormGroupControlConfigProp<TFormModelProp> = TFormModelProp extends readonly unknown[]
  ? FormControl<TFormModelProp> | PlatformFormGroupControlConfigPropArray<ArrayElement<TFormModelProp>>
  : FormControl<TFormModelProp>;

export type PlatformFormGroupControlConfigPropArray<TItemModel> = {
  modelItems: () => TItemModel[];
  itemControl: (
    item: TItemModel,
    itemIndex: number
  ) => PlatformPartialFormGroupControls<TItemModel> | FormControl<TItemModel>;
};

export type PlatformFormGroupControls<TFormModel> = {
  [P in keyof TFormModel]: TFormModel[P] extends readonly unknown[]
    ?
        | FormControl<TFormModel[P]>
        | FormArray<FormControl<ArrayElement<TFormModel[P]>>>
        | FormArray<FormGroup<PlatformFormGroupControls<ArrayElement<TFormModel[P]>>>>
    : FormControl<TFormModel[P]>;
};

export type PlatformPartialFormGroupControls<TFormModel> = {
  [P in keyof TFormModel]?: TFormModel[P] extends readonly unknown[]
    ?
        | FormControl<TFormModel[P]>
        | FormArray<FormControl<ArrayElement<TFormModel[P]>>>
        | FormArray<FormGroup<PlatformFormGroupControls<ArrayElement<TFormModel[P]>>>>
    : FormControl<TFormModel[P]>;
};

// Need to be code duplicated used in "export type PlatformFormGroupControls<TFormModel> = {", "export type PlatformPartialFormGroupControls<TFormModel> = {"
// "[P in keyof TFormModel]: TFormModel[P] ..." should be equal to PlatformFormGroupControlProp<TFormModel[P]>
// dont know why it will get type errors when using if TFormModel[P] is enum, boolean
export type PlatformFormGroupControlProp<TFormModelProp> = TFormModelProp extends readonly unknown[]
  ?
      | FormControl<TFormModelProp>
      | FormArray<FormControl<ArrayElement<TFormModelProp>>>
      | FormArray<FormGroup<PlatformFormGroupControls<ArrayElement<TFormModelProp>>>>
  : FormControl<TFormModelProp>;