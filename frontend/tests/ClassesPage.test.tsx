import { render, screen } from '@testing-library/react';
import { vi } from 'vitest';
import { ClassesPage } from '../src/pages/ClassesPage';

vi.mock('../src/api/classesApi', () => ({
  listClasses: vi.fn().mockResolvedValue([
    { id: 'c_1', name: 'Class 6', shift: 'Morning-Shift', totalSeats: 120, heldSeats: 15, confirmedSeats: 80, availableSeats: 25 },
    { id: 'c_2', name: 'Class 6', shift: 'Day-Shift', totalSeats: 60, heldSeats: 0, confirmedSeats: 60, availableSeats: 0 },
  ]),
}));
vi.mock('../src/api/holdsApi', () => ({ applyToClass: vi.fn() }));

describe('ClassesPage', () => {
  it('shows every class with its shift and seat counts', async () => {
    render(<ClassesPage studentId="" isValid={false} navigate={vi.fn()} />);
    expect(await screen.findByText('Morning-Shift')).toBeInTheDocument();
    expect(screen.getByText('Day-Shift')).toBeInTheDocument();
    expect(screen.getByText('25')).toBeInTheDocument();
  });

  it('disables Apply without a valid student id', async () => {
    render(<ClassesPage studentId="" isValid={false} navigate={vi.fn()} />);
    await screen.findByText('Morning-Shift');
    screen.getAllByRole('button', { name: 'Apply' }).forEach((b) => expect(b).toBeDisabled());
  });

  it('enables Apply only for classes that still have seats', async () => {
    render(<ClassesPage studentId="20152691234567890" isValid navigate={vi.fn()} />);
    await screen.findByText('Morning-Shift');
    const [morning, day] = screen.getAllByRole('button', { name: 'Apply' });
    expect(morning).toBeEnabled();
    expect(day).toBeDisabled();
  });

  it('has no way to add a class on the public list', async () => {
    render(<ClassesPage studentId="" isValid={false} navigate={vi.fn()} />);
    await screen.findByText('Morning-Shift');
    expect(screen.queryByRole('button', { name: /add class/i })).not.toBeInTheDocument();
  });
});
