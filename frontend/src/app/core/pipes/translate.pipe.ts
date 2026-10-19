import { Pipe, PipeTransform } from '@angular/core';
import { LocalizationService } from '../services/localization.service';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false // Impure pipe to immediately capture reactive language signal changes
})
export class TranslatePipe implements PipeTransform {
  constructor(private localizationService: LocalizationService) {}

  transform(key: string, params?: Record<string, any>): string {
    return this.localizationService.translate(key, params);
  }
}
