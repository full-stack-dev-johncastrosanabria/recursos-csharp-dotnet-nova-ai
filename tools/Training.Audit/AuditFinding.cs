namespace Training.Audit;

/// <summary>One thing that is wrong with the repository.</summary>
/// <param name="Check">Which check produced this, e.g. "pairs".</param>
/// <param name="Path">Repo-relative path, or another identifier for the subject
/// of the finding, such as a test class name.</param>
/// <param name="Message">What is wrong, in terms the author can act on.</param>
public sealed record AuditFinding(string Check, string Path, string Message);
