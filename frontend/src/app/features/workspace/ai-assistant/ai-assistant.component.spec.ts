import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AiAssistantComponent } from './ai-assistant.component';
import { AiAssistantStore } from '../../../core/stores/ai-assistant.store';
import { MOCK_PROMPT_CHIPS, MOCK_AI_RESPONSES } from '../../../core/mocks/f4-mock-data';

describe('AiAssistantComponent', () => {
  let component: AiAssistantComponent;
  let fixture: ComponentFixture<AiAssistantComponent>;
  let store: AiAssistantStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AiAssistantComponent, HttpClientTestingModule],
      providers: [AiAssistantStore]
    }).compileComponents();

    fixture = TestBed.createComponent(AiAssistantComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(AiAssistantStore);

    // Seed mock data
    store.promptChips.set([...MOCK_PROMPT_CHIPS]);

    fixture.detectChanges();
  });

  it('should create the AI assistant component', () => {
    expect(component).toBeTruthy();
    expect(store.promptChips().length).toBe(MOCK_PROMPT_CHIPS.length);
  });

  it('should trigger store.askAi on query submit', () => {
    spyOn(store, 'askAi');
    component.onQuerySubmitted('من هم الطلاب الأكثر غياباً؟');
    expect(store.askAi).toHaveBeenCalledWith('من هم الطلاب الأكثر غياباً؟');
  });

  it('should trigger store.askAi on prompt chip selection', () => {
    spyOn(store, 'askAi');
    const chip = MOCK_PROMPT_CHIPS[0];
    component.onChipSelected(chip);
    expect(store.askAi).toHaveBeenCalledWith(chip.prompt || chip.promptText || '');
  });

  it('should dismiss response when dismissCurrent is called', () => {
    store.currentResponse.set(MOCK_AI_RESPONSES['من هم الطلاب الأكثر غياباً؟']);
    expect(store.currentResponse()).not.toBeNull();

    component.dismissCurrent();
    expect(store.currentResponse()).toBeNull();
  });
});
