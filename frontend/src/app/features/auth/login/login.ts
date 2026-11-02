import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth-service';


@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatProgressBarModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {

  }

  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  private readonly fb: FormBuilder = new FormBuilder();

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.auth
      .login(email, password)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => this.router.navigateByUrl(this.returnUrl()),
        error: () => this.errorMessage.set('Invalid email or password.')
      });
  }

  /** Only accept in-app paths, never an absolute URL from the query string. */
  private returnUrl(): string {
    const requested = this.route.snapshot.queryParamMap.get('returnUrl');
    return requested?.startsWith('/') ? requested : '/';
  }
}