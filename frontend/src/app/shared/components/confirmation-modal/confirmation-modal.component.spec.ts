/**
 * Unit Tests for ConfirmationModalComponent
 * Milestone F2 / Principle 7: Destructive confirmation with mandatory reason
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmationModalComponent } from './confirmation-modal.component';
import { By } from '@angular/platform-browser';

describe('ConfirmationModalComponent', () => {
  let component: ConfirmationModalComponent;
  let fixture: ComponentFixture<ConfirmationModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmationModalComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmationModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should not render dialog when isOpen is false', () => {
    component.isOpen = false;
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('.modal-dialog'));
    expect(dialog).toBeNull();
  });

  it('should render and disable confirm button when requireReason is true and input < 5 chars', () => {
    component.isOpen = true;
    component.title = 'تأكيد إلغاء الحصة';
    component.message = 'هل أنت متأكد من رغبتك في إلغاء هذه الحصة؟';
    component.requireReason = true;
    component.reasonMinLength = 5;
    fixture.detectChanges();

    const confirmBtn = fixture.debugElement.query(By.css('.btn-confirm'));
    expect(confirmBtn.nativeElement.disabled).toBeTrue();

    component.reason = 'غلط';
    fixture.detectChanges();
    expect(component.isConfirmDisabled).toBeTrue();
    expect(confirmBtn.nativeElement.disabled).toBeTrue();

    component.reason = 'خطأ في الموعد المحدد';
    fixture.detectChanges();
    expect(component.isConfirmDisabled).toBeFalse();
    expect(confirmBtn.nativeElement.disabled).toBeFalse();
  });

  it('should emit confirmed with reason string on confirmation', () => {
    spyOn(component.confirmed, 'emit');
    component.isOpen = true;
    component.requireReason = true;
    component.reason = 'سبب كافي ومقبول للإلغاء';
    fixture.detectChanges();

    const confirmBtn = fixture.debugElement.query(By.css('.btn-confirm'));
    confirmBtn.nativeElement.click();

    expect(component.confirmed.emit).toHaveBeenCalledWith('سبب كافي ومقبول للإلغاء');
  });

  it('should close on Escape keydown', () => {
    spyOn(component.cancelled, 'emit');
    component.isOpen = true;
    fixture.detectChanges();

    const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
    window.dispatchEvent(escapeEvent);

    expect(component.cancelled.emit).toHaveBeenCalled();
  });
});
