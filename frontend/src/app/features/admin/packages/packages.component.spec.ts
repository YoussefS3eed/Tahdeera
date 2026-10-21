import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { PackagesComponent } from './packages.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { MOCK_PACKAGE_TEMPLATES, MOCK_ADMIN_WORKSPACES } from '../../../core/mocks/admin-mock-data';

describe('PackagesComponent (SCR-22)', () => {
  let component: PackagesComponent;
  let fixture: ComponentFixture<PackagesComponent>;
  let adminStore: AdminStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PackagesComponent],
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PackagesComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.packages.set(MOCK_PACKAGE_TEMPLATES);
    adminStore.workspaces.set(MOCK_ADMIN_WORKSPACES);
    fixture.detectChanges();
  });

  it('should render 3 tier cards', () => {
    const cards = fixture.nativeElement.querySelectorAll('.tier-card');
    expect(cards.length).toBe(3);
  });

  it('should implement Selective Merge (overrides win)', () => {
    const ws = MOCK_ADMIN_WORKSPACES[0];
    component.openOverridesModal(ws);
    expect(component.targetWorkspace).toEqual(ws);
    expect(component.activeOverrides.maxStudents).toBeDefined();
  });
});
