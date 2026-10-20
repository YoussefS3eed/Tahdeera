import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Student360Profile } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-profile-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-header.component.html',
  styleUrls: ['./profile-header.component.scss']
})
export class ProfileHeaderComponent {
  readonly profile = input.required<Student360Profile>();

  readonly manageCardClicked = output<void>();
  readonly editProfileClicked = output<void>();
}
