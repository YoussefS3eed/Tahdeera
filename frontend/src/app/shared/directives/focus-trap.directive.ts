/**
 * Focus Trap Directive
 * Milestone F1 Phase 7: WCAG 2.1 AA Keyboard Focus Entrapment for Modals and Dialogs
 */

import {
  Directive,
  ElementRef,
  EventEmitter,
  HostListener,
  Output,
  AfterViewInit,
  OnDestroy
} from '@angular/core';

@Directive({
  selector: '[appFocusTrap]',
  standalone: true
})
export class FocusTrapDirective implements AfterViewInit, OnDestroy {
  @Output() escapePressed = new EventEmitter<void>();

  private previouslyFocusedElement: HTMLElement | null = null;
  private readonly focusableSelector =
    'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

  constructor(private readonly el: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    if (typeof document !== 'undefined') {
      this.previouslyFocusedElement = document.activeElement as HTMLElement;
      this.focusInitialElement();
    }
  }

  ngOnDestroy(): void {
    if (this.previouslyFocusedElement && typeof this.previouslyFocusedElement.focus === 'function') {
      this.previouslyFocusedElement.focus();
    }
  }

  /**
   * Traps Tab navigation strictly within the host element.
   */
  @HostListener('keydown', ['$event'])
  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.escapePressed.emit();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const focusableElements = this.getFocusableElements();
    if (focusableElements.length === 0) {
      event.preventDefault();
      return;
    }

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];
    const activeElement = document.activeElement;

    if (event.shiftKey) {
      // Shift + Tab: moving backwards
      if (activeElement === firstElement || !this.el.nativeElement.contains(activeElement)) {
        event.preventDefault();
        lastElement.focus();
      }
    } else {
      // Tab: moving forwards
      if (activeElement === lastElement || !this.el.nativeElement.contains(activeElement)) {
        event.preventDefault();
        firstElement.focus();
      }
    }
  }

  /**
   * Focuses the first focusable element inside the trapped container.
   */
  focusInitialElement(): void {
    const focusableElements = this.getFocusableElements();
    if (focusableElements.length > 0) {
      focusableElements[0].focus();
    } else {
      this.el.nativeElement.focus();
    }
  }

  private getFocusableElements(): HTMLElement[] {
    const elements = this.el.nativeElement.querySelectorAll<HTMLElement>(this.focusableSelector);
    return Array.from(elements).filter(el => {
      return el.offsetWidth > 0 || el.offsetHeight > 0 || el.getClientRects().length > 0;
    });
  }
}
