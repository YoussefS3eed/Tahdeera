/**
 * Unit Tests for EmptyStateComponent
 * Milestone F2 / Principle 7: Dual-Mode Zero-State Component Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmptyStateComponent } from './empty-state.component';
import { By } from '@angular/platform-browser';

describe('EmptyStateComponent', () => {
  let component: EmptyStateComponent;
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(EmptyStateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and render in zero-data mode by default', () => {
    component.title = 'لا يوجد طلاب';
    component.description = 'قم بإضافة طلاب للبدء في تسجيل الحضور';
    component.actionLabel = '+ إضافة طالب جديد';
    fixture.detectChanges();

    expect(component).toBeTruthy();
    const titleEl = fixture.debugElement.query(By.css('.empty-state-title'));
    expect(titleEl.nativeElement.textContent).toContain('لا يوجد طلاب');

    const btn = fixture.debugElement.query(By.css('.btn-primary'));
    expect(btn).toBeTruthy();
    expect(btn.nativeElement.textContent).toContain('+ إضافة طالب جديد');
  });

  it('should render in zero-match mode with secondary action button', () => {
    component.mode = 'zero-match';
    component.title = 'لا توجد نتائج مطابقة';
    component.actionLabel = 'إعادة ضبط الفلاتر';
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn-secondary'));
    expect(btn).toBeTruthy();
    expect(btn.nativeElement.textContent).toContain('إعادة ضبط الفلاتر');
  });

  it('should emit actionClicked event when button is clicked', () => {
    spyOn(component.actionClicked, 'emit');
    component.actionLabel = 'تأكيد';
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.empty-state-btn'));
    btn.nativeElement.click();
    expect(component.actionClicked.emit).toHaveBeenCalled();
  });
});
