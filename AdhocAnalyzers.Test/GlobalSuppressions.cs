using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("General", "RCS1118:Mark local variable as const.", Justification = "Otherwise 'var source' won't work.")]
[assembly: SuppressMessage("Performance", "RCS1121:Use [] instead of calling 'First'.", Justification = "Premature optimization")]
[assembly: SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage", Justification = "<Pending>")]