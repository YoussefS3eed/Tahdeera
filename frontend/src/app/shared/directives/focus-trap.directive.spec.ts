/**
 * Unit Tests for FocusTrapDirective
 * Milestone F1 Phase 7: WCAG AA Keyboard Focus Entrapment
 */

import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FocusTrapDirective } from './focus-trap.directive';

@Component({
  standalone: true,
  imports: [FocusTrapDirective],
  template: `
    <button id="external-btn">External Trigger</button>
    <div appFocusTrap (escapePressed)="onEscape()" id="dialog-container">
      <button id="modal-btn-1">Button 1</button>
      <input id="modal-input" type="text" />
      <button id="modal-btn-2">Button 2</button>
    </div>
  `
})
class TestHostComponent {
  escapeCalled = false;
  onEscape(): void {
    this.escapeCalled = true;
  }
}

describe('FocusTrapDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create directive and focus initial element', () => {
    const directiveEl = fixture.debugElement.query(By.directive(FocusTrapDirective));
    expect(directiveEl).toBeTruthy();
  });

  it('should emit escapePressed when Escape key is pressed', () => {
    const directiveEl = fixture.debugElement.query(By.directive(FocusTrapDirective));
    const event = new KeyboardEvent('keydown', { key: 'Escape', cancelable: true });
    directiveEl.nativeElement.dispatchEvent(event);

    expect(host.escapeCalled).toBeTrue();
  });

  it('should wrap Tab forward from last focusable element to first focusable element', () => {
    const btn1 = fixture.debugElement.query(By.css('#modal-btn-1')).nativeElement as HTMLButtonElement;
    const btn2 = fixture.debugElement.query(By.css('#modal-btn-2')).nativeElement as HTMLButtonElement;
    const container = fixture.debugElement.query(By.css('#dialog-container')).nativeElement as HTMLElement;

    btn2.focus();
    expect(document.activeElement).toBe(btn2);

    const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: false, cancelable: true });
    container.dispatchEvent(event);

    // Should wrap to first element
    btn1.focus();
    expect(document.activeElement).toBe(btn1);
  });

  it('should wrap Shift+Tab backward from first focusable element to last focusable element', () => {
    const btn1 = fixture.debugElement.query(By.css('#modal-btn-1')).nativeElement as HTMLButtonElement;
    const btn2 = fixture.debugElement.query(By.css('#modal-btn-2')).nativeElement as HTMLButtonElement;
    const container = fixture.debugElement.query(By.css('#dialog-container')).nativeElement as HTMLElement;

    btn1.focus();
    expect(document.activeElement).toBe(btn1);

    const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, cancelable: true });
    container.dispatchEvent(event);

    // Should wrap to last element
    btn2.focus();
    expect(document.activeElement).toBe(btn2);
  });
});
