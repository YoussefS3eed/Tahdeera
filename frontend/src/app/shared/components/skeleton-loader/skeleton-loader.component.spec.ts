/**
 * Unit Tests for SkeletonLoaderComponent
 * Milestone F1 Phase 7: Constitution Principle 7 Loading Placeholder Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkeletonLoaderComponent } from './skeleton-loader.component';
import { By } from '@angular/platform-browser';

describe('SkeletonLoaderComponent', () => {
  let component: SkeletonLoaderComponent;
  let fixture: ComponentFixture<SkeletonLoaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkeletonLoaderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SkeletonLoaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create with default text shape', () => {
    expect(component).toBeTruthy();
    expect(component.type).toBe('text');
    const item = fixture.debugElement.query(By.css('.skeleton-text'));
    expect(item).toBeTruthy();
  });

  it('should render multiple items when count > 1', () => {
    component.count = 3;
    fixture.detectChanges();

    const items = fixture.debugElement.queryAll(By.css('.skeleton-item'));
    expect(items.length).toBe(3);
  });

  it('should support card, table-row, and circle shapes', () => {
    component.type = 'card';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.skeleton-card'))).toBeTruthy();

    component.type = 'table-row';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.skeleton-table-row'))).toBeTruthy();

    component.type = 'circle';
    component.width = '40px';
    fixture.detectChanges();
    const circle = fixture.debugElement.query(By.css('.skeleton-circle'));
    expect(circle).toBeTruthy();
    expect(component.itemHeight).toBe('40px');
  });
});
