export function Loader({ label = 'Loading...' }: { label?: string }) {
  return (
    <p className="loader" role="status">
      {label}
    </p>
  );
}
