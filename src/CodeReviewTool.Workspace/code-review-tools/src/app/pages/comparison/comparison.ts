import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ComparisonInput, ComparisonRequest } from 'code-review-tool-components';
import { ComparisonService } from '../../services/comparison.service';

@Component({
  selector: 'app-comparison',
  imports: [ComparisonInput],
  templateUrl: './comparison.html',
  styleUrl: './comparison.scss',
})
export class Comparison {
  private _comparisonService = inject(ComparisonService);
  private _router = inject(Router);

  onCompare(request: ComparisonRequest): void {
    this._comparisonService.initiateComparison(request);
    this._router.navigate(['/review']);
  }
}
