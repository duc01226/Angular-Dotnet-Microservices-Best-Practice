/* eslint-disable @typescript-eslint/no-explicit-any */
export interface SimpleChange<T> {
    previousValue: T;
    currentValue: T;
}

export type WatchCallBackFunction<T, TTargetObj> = (value: T, change: SimpleChange<T>, targetObj: TTargetObj) => void;

/**
 * Operator used to watch a property when it is set
 *
 * Example:
 *
 * // Shorthand execute a target function doing something directly if on change only do this logic
 * @Watch('pagedResultWatch')
 * public pagedResult?: PlatformPagedResultDto<LeaveType>;
 *
 * // Full syntax execute a NORMAL FUNCTION
 * @Watch<LeaveTypesState, PlatformPagedQueryDto>((value, change, targetObj) => {
 *   targetObj.updatePageInfo();
 * })
 * public pagedQuery: PlatformPagedQueryDto = new PlatformPagedQueryDto();
 *
 * public pagedResultWatch(
 *   value: PlatformPagedResultDto<LeaveType> | undefined,
 *   change: SimpleChange<PlatformPagedResultDto<LeaveType> | undefined>
 * ) {
 *   this.updatePageInfo();
 * }
 */
export function Watch<TTargetObj extends object = object, TProp = object>(
    callbackFnOrName: WatchCallBackFunction<TProp, TTargetObj> | keyof TTargetObj,
    onlyWhen?: (obj: TTargetObj) => boolean,
    afterCallback?: (target: TTargetObj) => void
) {
    return (target: TTargetObj, key: keyof TTargetObj) => {
        EnsureNotExistingSetterForKey(target, key);

        const privatePropKey = `_${key.toString()}`;

        Object.defineProperty(target, key, {
            set: function (value: TProp) {
                const oldValue = this[privatePropKey];
                this[privatePropKey] = value;

                if (onlyWhen != null && !onlyWhen(this)) return;

                const simpleChange: SimpleChange<TProp> = {
                    previousValue: oldValue,
                    currentValue: this[privatePropKey]
                };

                if (typeof callbackFnOrName === 'string') {
                    const callBackMethod = (target as any)[callbackFnOrName];
                    if (callBackMethod == null) {
                        throw new Error(`Cannot find method ${callbackFnOrName} in class ${target.constructor.name}`);
                    }

                    callBackMethod.call(this, this[privatePropKey], simpleChange, this);
                } else if (typeof callbackFnOrName == 'function') {
                    callbackFnOrName(this[privatePropKey], simpleChange, this);
                }

                if (afterCallback != null) afterCallback(this);
            },
            get: function () {
                return this[privatePropKey];
            },
            enumerable: true,
            configurable: true
        });
    };

    function EnsureNotExistingSetterForKey<TTargetObj extends object>(target: TTargetObj, key: PropertyKey) {
        const existingTargetKeyProp = Object.getOwnPropertyDescriptors(target)[key.toString()];

        if (existingTargetKeyProp?.set != null || existingTargetKeyProp?.get != null)
            throw Error(
                'Could not use watch decorator on a existing get/set property. Should only use one solution, either get/set property or @Watch decorator'
            );
    }
}
