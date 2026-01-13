import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { CommentData as CommentModel } from '../models/diff.model';

@Component({
  selector: 'crt-code-comment',
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule
  ],
  templateUrl: './code-comment.html',
  styleUrl: './code-comment.scss',
})
export class CodeComment {
  comment = input.required<CommentModel>();
  isReplyFormVisible = input<boolean>(false);

  reply = output<{ commentId: string; content: string }>();
  resolve = output<string>();

  replyText = '';

  onReply(): void {
    if (this.replyText.trim()) {
      this.reply.emit({
        commentId: this.comment().commentId,
        content: this.replyText
      });
      this.replyText = '';
    }
  }

  onResolve(): void {
    this.resolve.emit(this.comment().commentId);
  }

  formatTimestamp(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - new Date(date).getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
  }
}
