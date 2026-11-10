import { fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';
import { ClassForm } from '../src/components/ClassForm';

function fill(name: string, seats: string) {
  fireEvent.change(screen.getByLabelText('Class name'), { target: { value: name } });
  fireEvent.change(screen.getByLabelText('Total seats'), { target: { value: seats } });
}

describe('ClassForm', () => {
  it('offers exactly the two predefined shifts', () => {
    render(<ClassForm busy={false} error={null} onSubmit={vi.fn()} />);
    const options = Array.from(screen.getByLabelText('Shift').querySelectorAll('option')).map((o) => o.textContent);
    expect(options).toEqual(['Select a shift', 'Morning-Shift', 'Day-Shift']);
  });

  it('will not submit until a shift is chosen', () => {
    const onSubmit = vi.fn();
    render(<ClassForm busy={false} error={null} onSubmit={onSubmit} />);
    fill('Class 6', '120');
    fireEvent.click(screen.getByRole('button', { name: 'Add class' }));
    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toHaveTextContent('Choose a shift.');
  });

  it('submits name, shift and seats', () => {
    const onSubmit = vi.fn();
    render(<ClassForm busy={false} error={null} onSubmit={onSubmit} />);
    fill('Class 6', '120');
    fireEvent.change(screen.getByLabelText('Shift'), { target: { value: 'Day-Shift' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add class' }));
    expect(onSubmit).toHaveBeenCalledWith('Class 6', 'Day-Shift', 120);
  });
});
