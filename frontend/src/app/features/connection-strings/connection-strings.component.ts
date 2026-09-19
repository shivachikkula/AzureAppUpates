import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { ApplicationsService } from '../../core/services/applications.service';
import { ConnectionStringsService } from '../../core/services/connection-strings.service';
import { AzureApplication, SaveConnectionStringResult } from '../../core/models/domain.models';

@Component({
  selector: 'app-connection-strings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './connection-strings.component.html',
  styleUrl: './connection-strings.component.scss',
})
export class ConnectionStringsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly applicationsService = inject(ApplicationsService);
  private readonly connectionStringsService = inject(ConnectionStringsService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly apps = signal<AzureApplication[]>([]);
  readonly loadingApps = signal(true);
  readonly saving = signal(false);
  readonly result = signal<SaveConnectionStringResult | null>(null);

  readonly form = this.fb.nonNullable.group({
    applicationId: ['', Validators.required],
    key: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9_.:-]+$/)]],
    value: ['', Validators.required],
    type: ['Custom' as const, Validators.required],
    reason: [''],
  });

  constructor() {
    this.applicationsService.getMyApplications().subscribe({
      next: (apps) => {
        this.apps.set(apps);
        this.loadingApps.set(false);
        const preselect = this.route.snapshot.queryParamMap.get('appId');
        if (preselect && apps.some((a) => a.id === preselect)) {
          this.form.controls.applicationId.setValue(preselect);
        }
      },
      error: () => this.loadingApps.set(false),
    });
  }

  get selectedApp(): AzureApplication | undefined {
    return this.apps().find((a) => a.id === this.form.controls.applicationId.value);
  }

  get isProduction(): boolean {
    return this.selectedApp?.environment === 'Production';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.result.set(null);
    const value = this.form.getRawValue();

    this.connectionStringsService.save(value).subscribe({
      next: (res) => {
        this.result.set(res);
        this.saving.set(false);
        this.snackBar.open(res.message, 'Dismiss', { duration: 5000 });
        this.form.patchValue({ key: '', value: '', reason: '' });
      },
      error: (err) => {
        this.saving.set(false);
        const message = err?.error?.message ?? 'Failed to save the connection string. Please try again.';
        this.snackBar.open(message, 'Dismiss', { duration: 6000 });
      },
    });
  }
}
