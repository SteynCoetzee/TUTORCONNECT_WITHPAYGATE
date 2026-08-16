import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';

describe('LoginComponent — Login Flow & Error Handling', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authSpy: jasmine.SpyObj<AuthService>;

  const validCredentials = { email: 'student@up.ac.za', password: 'Password1' };

  beforeEach(async () => {
    authSpy = jasmine.createSpyObj('AuthService', [
      'login', 'logout', 'isLoggedIn', 'getToken',
      'stopInactivityTimer', 'startInactivityTimer', 'resetInactivityTimer'
    ]);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, RouterTestingModule.withRoutes([])],
      providers: [{ provide: AuthService, useValue: authSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialise with empty credentials, no error, and not loading', () => {
    expect(component.loginObj.email).toBe('');
    expect(component.loginObj.password).toBe('');
    expect(component.errorMessage).toBe('');
    expect(component.loading).toBeFalse();
  });

  it('should call authService.login with the entered credentials', () => {
    authSpy.login.and.returnValue(of('fake.jwt.token'));
    component.loginObj = { ...validCredentials };
    component.onLogin();
    expect(authSpy.login).toHaveBeenCalledWith(validCredentials);
  });

  it('should clear a previous error message before each new login attempt', () => {
    authSpy.login.and.returnValue(of('token'));
    component.loginObj = { ...validCredentials };
    component.errorMessage = 'Previous error';
    component.onLogin();
    expect(component.errorMessage).toBe('');
  });

  it('should show a connection error when the API is unreachable (status 0)', () => {
    authSpy.login.and.returnValue(throwError(() => ({ status: 0 })));
    component.loginObj = { ...validCredentials };
    component.onLogin();
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe(
      'Unable to reach the server. Please check your connection.'
    );
  });

  it('should display a string server error message for a 400 Bad Request', () => {
    authSpy.login.and.returnValue(
      throwError(() => ({ status: 400, error: 'Invalid email or password.' }))
    );
    component.loginObj = { ...validCredentials };
    component.onLogin();
    expect(component.errorMessage).toBe('Invalid email or password.');
  });

  it('should show the incorrect credentials message for a 401 Unauthorised', () => {
    authSpy.login.and.returnValue(
      throwError(() => ({ status: 401, error: 'Unauthorised' }))
    );
    component.loginObj = { ...validCredentials };
    component.onLogin();
    expect(component.errorMessage).toBe('Incorrect email or password. Please try again.');
  });

  it('should set loading to false after a failed login attempt', () => {
    authSpy.login.and.returnValue(throwError(() => ({ status: 500, error: 'Server error' })));
    component.loginObj = { ...validCredentials };
    component.onLogin();
    expect(component.loading).toBeFalse();
  });
});
