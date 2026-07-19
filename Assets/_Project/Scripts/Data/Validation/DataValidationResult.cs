#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Data.Definitions;

namespace RandomTowerDefense.Data.Validation
{
    public sealed class DataValidationIssue
    {
        public DataValidationIssue(string code, string message, DefinitionAsset? source)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Source = source;
        }

        public string Code { get; }

        public string Message { get; }

        public DefinitionAsset? Source { get; }

        public override string ToString()
        {
            return $"[{Code}] {Message}";
        }
    }

    public sealed class DataValidationResult
    {
        public DataValidationResult(IReadOnlyList<DataValidationIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            var copy = new DataValidationIssue[issues.Count];
            for (int index = 0; index < issues.Count; index++)
            {
                copy[index] = issues[index]
                    ?? throw new ArgumentException("Validation issues must not contain null.", nameof(issues));
            }

            Issues = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<DataValidationIssue> Issues { get; }

        public bool IsValid => Issues.Count == 0;
    }
}
