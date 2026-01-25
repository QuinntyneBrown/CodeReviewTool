import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { DiffLine } from '../models/diff.model';

@Component({
  selector: 'crt-code-line',
  imports: [
    CommonModule,
    MatIconModule
  ],
  templateUrl: './code-line.html',
  styleUrl: './code-line.scss',
})
export class CodeLine {
  line = input.required<DiffLine>();
  showCommentButton = input<boolean>(true);
  hasComment = input<boolean>(false);

  addComment = output<number>();

  onAddComment(): void {
    this.addComment.emit(this.line().lineNumber);
  }
}
