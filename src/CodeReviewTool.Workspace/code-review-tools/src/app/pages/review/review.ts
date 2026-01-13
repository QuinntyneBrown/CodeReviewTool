import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DiffViewer } from 'code-review-tool-components';
import { ComparisonService } from '../../services/comparison.service';
import { map } from 'rxjs';

@Component({
  selector: 'app-review',
  imports: [CommonModule, DiffViewer],
  templateUrl: './review.html',
  styleUrl: './review.scss',
})
export class Review {
  private _comparisonService = inject(ComparisonService);

  viewModel$ = this._comparisonService.comparisonResult$.pipe(
    map(result => ({
      result,
      isLoading: !result
    }))
  );
}
