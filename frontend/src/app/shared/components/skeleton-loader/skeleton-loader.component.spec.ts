/**
 * Unit Tests for SkeletonLoaderComponent
 * Milestone F2 / Principle 7: Loading Placeholder Tests with Fixed Dimensions
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

  it('should create with default kpi-card variant and 124px height', () => {
    expect(component).toBeTruthy();
    expect(component.variant).toBe('kpi-card');
    const item = fixture.debugElement.query(By.css('.skeleton-kpi-card'));
    expect(item).toBeTruthy();
    expect(component.itemHeight).toBe('124px');
  });

  it('should render multiple items when count > 1', () => {
    component.count = 5;
    fixture.detectChanges();

    const items = fixture.debugElement.queryAll(By.css('.skeleton-item'));
    expect(items.length).toBe(5);
  });

  it('should support table-row variant with exact 52px height', () => {
    component.variant = 'table-row';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.skeleton-table-row'))).toBeTruthy();
    expect(component.itemHeight).toBe('52px');
  });

  it('should support legacy shape type input for backwards compatibility', () => {
    component.type = 'card';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.skeleton-kpi-card'))).toBeTruthy();
    expect(component.itemHeight).toBe('124px');

    component.type = 'circle';
    component.width = '40px';
    fixture.detectChanges();
    const circle = fixture.debugElement.query(By.css('.skeleton-avatar'));
    expect(circle).toBeTruthy();
    expect(component.itemHeight).toBe('40px');
  });
});
