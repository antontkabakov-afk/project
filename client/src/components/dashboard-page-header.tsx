interface DashboardPageHeaderProps {
  eyebrow: string;
  title: string;
  description: string;
}

export default function DashboardPageHeader({
  eyebrow,
  title,
  description,
}: DashboardPageHeaderProps) {
  return (
    <header className="mb-8 space-y-3">
      <p className="text-xs font-semibold uppercase tracking-[0.45em] text-[#00F5C8]/80">
        {eyebrow}
      </p>
      <h1 className="text-3xl font-semibold text-white md:text-4xl">{title}</h1>
      <p className="max-w-2xl text-sm leading-6 text-white/65">{description}</p>
    </header>
  );
}
