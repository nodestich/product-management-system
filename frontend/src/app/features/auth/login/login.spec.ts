import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { Login } from './login';
import { AuthService } from '../../../../core/auth/auth-service';

describe('Login', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { login: () => of({ id: 'user-id', email: 'admin@example.com' }) }
        }
      ]
    }).compileComponents();
  });

  it('starts with an invalid form', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();

    const form = fixture.componentInstance.form;
    expect(form.invalid).toBe(true);
    expect(form.controls.email.hasError('required')).toBe(true);
    expect(form.controls.password.hasError('required')).toBe(true);
  });

  it('rejects a malformed email before submitting', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();

    const form = fixture.componentInstance.form;
    form.controls.email.setValue('not-an-email');
    form.controls.password.setValue('whatever');

    expect(form.controls.email.hasError('email')).toBe(true);
    expect(form.valid).toBe(false);
  });
});