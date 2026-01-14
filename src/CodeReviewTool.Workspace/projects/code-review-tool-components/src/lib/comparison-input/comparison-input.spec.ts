import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ComparisonInput } from './comparison-input';

describe('ComparisonInput', () => {
  let component: ComparisonInput;
  let fixture: ComponentFixture<ComparisonInput>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ComparisonInput,
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        BrowserAnimationsModule
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ComparisonInput);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default values', () => {
    expect(component.repositoryPath).toBe('');
    expect(component.fromBranch).toBe('main');
    expect(component.intoBranch).toBe('');
  });

  it('should emit compare event when form is valid', () => {
    const emitSpy = jest.spyOn(component.compare, 'emit');
    
    component.repositoryPath = '/path/to/repo';
    component.fromBranch = 'main';
    component.intoBranch = 'feature/test';
    
    component.onCompare();
    
    expect(emitSpy).toHaveBeenCalledWith({
      repositoryPath: '/path/to/repo',
      fromBranch: 'main',
      intoBranch: 'feature/test'
    });
  });

  it('should not emit compare event when repository path is empty', () => {
    const emitSpy = jest.spyOn(component.compare, 'emit');
    
    component.repositoryPath = '';
    component.fromBranch = 'main';
    component.intoBranch = 'feature/test';
    
    component.onCompare();
    
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should not emit compare event when from branch is empty', () => {
    const emitSpy = jest.spyOn(component.compare, 'emit');
    
    component.repositoryPath = '/path/to/repo';
    component.fromBranch = '';
    component.intoBranch = 'feature/test';
    
    component.onCompare();
    
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should not emit compare event when into branch is empty', () => {
    const emitSpy = jest.spyOn(component.compare, 'emit');
    
    component.repositoryPath = '/path/to/repo';
    component.fromBranch = 'main';
    component.intoBranch = '';
    
    component.onCompare();
    
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should clear form when onClear is called', () => {
    component.repositoryPath = '/path/to/repo';
    component.fromBranch = 'develop';
    component.intoBranch = 'feature/test';
    
    component.onClear();
    
    expect(component.repositoryPath).toBe('');
    expect(component.fromBranch).toBe('main');
    expect(component.intoBranch).toBe('');
  });

  it('should have compare button disabled when fields are empty', () => {
    const compiled = fixture.nativeElement;
    const compareButton = compiled.querySelector('button[type="submit"]');
    
    expect(compareButton.disabled).toBe(true);
  });

  it('should enable compare button when all fields are filled', async () => {
    component.repositoryPath = '/path/to/repo';
    component.fromBranch = 'main';
    component.intoBranch = 'feature/test';
    fixture.detectChanges();
    await fixture.whenStable();
    
    const compiled = fixture.nativeElement;
    const compareButton = compiled.querySelector('button[type="submit"]');
    
    expect(compareButton.disabled).toBe(false);
  });
});
