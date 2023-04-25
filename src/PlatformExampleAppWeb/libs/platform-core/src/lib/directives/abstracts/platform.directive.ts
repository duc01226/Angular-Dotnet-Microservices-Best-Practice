import { AfterViewInit, ChangeDetectorRef, Directive, OnDestroy, OnInit } from '@angular/core';
import { BehaviorSubject, MonoTypeOperatorFunction, Subscription } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';

import { task_delay } from '../../utils';

@Directive()
export abstract class PlatformDirective implements OnInit, AfterViewInit, OnDestroy {
    public static get defaultDetectChangesDelay(): number {
        return 100;
    }

    public constructor(protected changeDetector: ChangeDetectorRef) {}

    public initiated$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
    public viewInitiated$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
    public destroyed$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

    private detectChangesDelaySubs: Subscription = new Subscription();

    public detectChanges(delayTime?: number, onDone?: () => unknown, checkParentForHostBinding: boolean = false): void {
        this.detectChangesDelaySubs.unsubscribe();
        if (!this.canDetectChanges) {
            return;
        }

        const finalDelayTime = delayTime == null ? PlatformDirective.defaultDetectChangesDelay : delayTime;
        this.detectChangesDelaySubs = task_delay(() => {
            if (this.canDetectChanges) {
                this.changeDetector.detectChanges();
                if (checkParentForHostBinding) {
                    this.changeDetector.markForCheck();
                }
                if (onDone != null) {
                    onDone();
                }
            }
        }, finalDelayTime);
    }

    public untilDestroyed<T>(): MonoTypeOperatorFunction<T> {
        return takeUntil(this.destroyed$.pipe(filter(destroyed => destroyed == true)));
    }

    public ngOnInit(): void {
        this.initiated$.next(true);
    }

    public ngAfterViewInit(): void {
        this.viewInitiated$.next(true);
    }

    public ngOnDestroy(): void {
        this.destroyed$.next(true);

        this.destroyAllSubjects();
    }

    protected get canDetectChanges(): boolean {
        return this.initiated$.value && !this.destroyed$.value;
    }

    private destroyAllSubjects(): void {
        this.initiated$.complete();
        this.viewInitiated$.complete();
        this.destroyed$.complete();
    }
}
