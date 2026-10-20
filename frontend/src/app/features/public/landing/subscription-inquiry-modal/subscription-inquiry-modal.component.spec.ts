/**
 * Unit Tests for SubscriptionInquiryModalComponent
 * Milestone F2 / SCR-01: Subscription Inquiry Modal Tests
 */

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { SubscriptionInquiryModalComponent } from './subscription-inquiry-modal.component';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';

describe('SubscriptionInquiryModalComponent', () => {
  let component: SubscriptionInquiryModalComponent;
  let fixture: ComponentFixture<SubscriptionInquiryModalComponent>;
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubscriptionInquiryModalComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SubscriptionInquiryModalComponent);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should not display modal dialog when isOpen is false', () => {
    component.isOpen = false;
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('.modal-dialog'));
    expect(dialog).toBeNull();
  });

  it('should display modal dialog with accessibility attributes when isOpen is true', () => {
    component.isOpen = true;
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('.modal-dialog'));
    expect(dialog).toBeTruthy();
    expect(dialog.attributes['role']).toBe('dialog');
    expect(dialog.attributes['aria-modal']).toBe('true');
  });

  it('should strictly validate Egyptian phone number regex', () => {
    const phoneControl = component.inquiryForm.get('phone');

    // Valid Egyptian mobile numbers (010, 011, 012, 015 + 8 digits = 11 digits)
    phoneControl?.setValue('01012345678');
    expect(phoneControl?.valid).toBeTrue();

    phoneControl?.setValue('01123456789');
    expect(phoneControl?.valid).toBeTrue();

    phoneControl?.setValue('01272785618');
    expect(phoneControl?.valid).toBeTrue();

    phoneControl?.setValue('01512345678');
    expect(phoneControl?.valid).toBeTrue();

    // Invalid prefix 013 (landline/invalid mobile carrier)
    phoneControl?.setValue('01312345678');
    expect(phoneControl?.valid).toBeFalse();

    // Invalid length (10 digits)
    phoneControl?.setValue('0101234567');
    expect(phoneControl?.valid).toBeFalse();

    // Invalid length (12 digits)
    phoneControl?.setValue('010123456789');
    expect(phoneControl?.valid).toBeFalse();

    // Non-numeric characters
    phoneControl?.setValue('0101234567a');
    expect(phoneControl?.valid).toBeFalse();
  });

  it('should require minimum 3 characters for name', () => {
    const nameControl = component.inquiryForm.get('name');
    nameControl?.setValue('أح');
    expect(nameControl?.valid).toBeFalse();

    nameControl?.setValue('أحمد');
    expect(nameControl?.valid).toBeTrue();
  });

  it('should submit inquiry and emit response on success', fakeAsync(() => {
    spyOn(component.inquirySubmitted, 'emit');
    component.isOpen = true;
    fixture.detectChanges();

    component.inquiryForm.setValue({
      name: 'أحمد محمود',
      phone: '01012345678',
      governorate: 'القاهرة',
      estimatedStudents: '100-300',
      selectedPackage: 'Pro'
    });

    component.onSubmit();
    expect(component.isSubmitting).toBeTrue();

    const req = httpTesting.expectOne('/api/v1/public/inquiries');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      name: 'أحمد محمود',
      phone: '01012345678',
      governorate: 'القاهرة',
      estimatedStudents: '100-300',
      selectedPackage: 'Pro'
    });

    req.flush({
      success: true,
      inquiryId: 'inq-101',
      message: 'تم استلام طلبكم بنجاح'
    });

    fixture.detectChanges();
    expect(component.isSubmitting).toBeFalse();
    expect(component.isSuccess).toBeTrue();
    expect(component.inquirySubmitted.emit).toHaveBeenCalled();

    tick(2000);
    expect(component.isOpen).toBeFalse();
  }));

  it('should close on Escape keydown', () => {
    spyOn(component.closed, 'emit');
    component.isOpen = true;
    fixture.detectChanges();

    const event = new KeyboardEvent('keydown', { key: 'Escape' });
    window.dispatchEvent(event);

    expect(component.isOpen).toBeFalse();
    expect(component.closed.emit).toHaveBeenCalled();
  });
});
