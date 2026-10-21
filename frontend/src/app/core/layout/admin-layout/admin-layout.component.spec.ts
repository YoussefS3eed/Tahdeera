import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminLayoutComponent } from './admin-layout.component';
import { AuthStore } from '../../stores/auth.store';

describe('AdminLayoutComponent', () => {
  let component: AdminLayoutComponent;
  let fixture: ComponentFixture<AdminLayoutComponent>;
  let authStore: AuthStore;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLayoutComponent],
      providers: [
        AuthStore,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminLayoutComponent);
    component = fixture.componentInstance;
    authStore = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should render 8 navigation tabs', () => {
    expect(component.navTabs.length).toBe(8);
    const tabs = fixture.nativeElement.querySelectorAll('.nav-tab');
    expect(tabs.length).toBe(8);
  });

  it('should logout and navigate to /login when logout button is clicked', () => {
    spyOn(authStore, 'logout');
    spyOn(router, 'navigate');
    component.logout();
    expect(authStore.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
