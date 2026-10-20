/**
 * Unit Tests for ReauthModalComponent
 * Milestone F2 / Principle 7 & Principle 9: In-place Re-authentication Overlay Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReauthModalComponent } from './reauth-modal.component';
import { By } from '@angular/platform-browser';

describe('ReauthModalComponent', () => {
  let component: ReauthModalComponent;
  let fixture: ComponentFixture<ReauthModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReauthModalComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ReauthModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render dialog with prefilled email when isOpen is true', () => {
    component.isOpen = true;
    component.userEmail = 'teacher@tahdeera.app';
    fixture.detectChanges();

    const emailInput = fixture.debugElement.query(By.css('#reauth-email'));
    expect(emailInput).toBeTruthy();
    expect(emailInput.nativeElement.value).toBe('teacher@tahdeera.app');
    expect(emailInput.nativeElement.disabled).toBeTrue();
  });

  it('should toggle password visibility', () => {
    component.isOpen = true;
    fixture.detectChanges();

    const pwdInput = fixture.debugElement.query(By.css('#reauth-password'));
    expect(pwdInput.nativeElement.type).toBe('password');

    component.togglePasswordVisibility();
    fixture.detectChanges();
    expect(pwdInput.nativeElement.type).toBe('text');
  });

  it('should emit submitPassword on form submit', () => {
    spyOn(component.submitPassword, 'emit');
    component.isOpen = true;
    component.password = 'SecretPass123';
    fixture.detectChanges();

    component.onSubmit();
    expect(component.submitPassword.emit).toHaveBeenCalledWith('SecretPass123');
  });

  it('should close and emit closed on Escape key', () => {
    spyOn(component.closed, 'emit');
    component.isOpen = true;
    fixture.detectChanges();

    const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
    window.dispatchEvent(escapeEvent);

    expect(component.closed.emit).toHaveBeenCalled();
    expect(component.isOpen).toBeFalse();
  });
});
