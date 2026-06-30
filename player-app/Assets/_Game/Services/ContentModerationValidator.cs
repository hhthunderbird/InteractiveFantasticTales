using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace InteractiveFantasticTales.Services
{
    /// <summary>
    /// Automated content moderation validator for story data.
    /// Checks text content, metadata, and section structure before publishing.
    /// Human review is still required — this is the first automated pass.
    /// </summary>
    public static class ContentModerationValidator
    {
        public enum Severity { Pass, Warning, Block }

        [Serializable]
        public class ModerationResult
        {
            public bool passed;
            public List<ModerationIssue> issues = new();
        }

        [Serializable]
        public class ModerationIssue
        {
            public Severity severity;
            public string code;
            public string message;
            public string location; // "metadata.title", "sections.42.text"
        }

        // --- Blocked patterns (regex, case-insensitive) ---
        private static readonly List<(Regex pattern, string code, string description)> BlockedPatterns = new()
        {
            // Hate speech indicators (simplified — production needs ML model)
            (new Regex(@"\b(?:matar todos os|exterminar|genocídio)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "HATE_001", "Potencial discurso de ódio detectado"),
            // Extreme violence against minors
            (new Regex(@"\b(?:criança.*morta|infanticídio|criança.*violen)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "CHILD_001", "Conteúdo envolvendo violência contra menores"),
            // Self-harm / suicide instruction
            (new Regex(@"\b(?:como se matar|método.*suicídio|instrução.*suicídio)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "SELF_001", "Conteúdo instrucional de autoagressão"),
            // CSAM indicators
            (new Regex(@"\b(?:criança.*sexual|pedofilia|infantil.*explícito)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "CSAM_001", "Indicador de conteúdo ilegal envolvendo menores"),
        };

        // --- Warning patterns ---
        private static readonly List<(Regex pattern, string code, string description)> WarningPatterns = new()
        {
            // Strong language
            (new Regex(@"\b(?:caralho|porra|foda-se|puta que pariu)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "LANG_001", "Linguagem forte detectada"),
            // Graphic violence description
            (new Regex(@"\b(?:sangue.*jorrando|tripas.*expostas|crânio.*esmagado|desmembrado)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "VIOL_001", "Descrição gráfica de violência"),
            // Drug use
            (new Regex(@"\b(?:injetar.*heroína|cheirar.*cocaína|fumar.*crack)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
             "DRUG_001", "Referência instrucional a uso de drogas"),
        };

        // --- Auto age-rating ---
        public static string CalculateAgeRating(List<ModerationIssue> issues)
        {
            foreach (var issue in issues)
            {
                if (issue.severity == Severity.Block) return "18+";
                if (issue.code.StartsWith("VIOL_")) return "16+";
                if (issue.code.StartsWith("LANG_")) return "14+";
            }
            return "Livre";
        }

        /// <summary>
        /// Validates story metadata for moderation compliance.
        /// </summary>
        public static ModerationResult ValidateMetadata(string title, string description, string authorName, string[] tags)
        {
            var result = new ModerationResult();

            // Check title
            if (string.IsNullOrWhiteSpace(title))
                result.issues.Add(new ModerationIssue { severity = Severity.Block, code = "META_001", message = "Título é obrigatório", location = "metadata.title" });
            else if (title.Length > 100)
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "META_002", message = "Título muito longo (>100 caracteres)", location = "metadata.title" });

            // Check description
            if (string.IsNullOrWhiteSpace(description))
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "META_003", message = "Descrição é recomendada", location = "metadata.description" });
            else if (description.Length > 500)
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "META_004", message = "Descrição muito longa (>500 caracteres)", location = "metadata.description" });

            // Check author name
            if (string.IsNullOrWhiteSpace(authorName))
                result.issues.Add(new ModerationIssue { severity = Severity.Block, code = "META_005", message = "Nome do autor é obrigatório", location = "metadata.author" });
            else if (authorName.Length > 80)
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "META_006", message = "Nome do autor muito longo", location = "metadata.author" });

            // Check tags
            if (tags == null || tags.Length == 0)
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "META_007", message = "Pelo menos uma tag de gênero é recomendada", location = "metadata.tags" });

            // Run pattern checks on text fields
            CheckPatterns(title, "metadata.title", result);
            CheckPatterns(description, "metadata.description", result);

            result.passed = !result.issues.Exists(i => i.severity == Severity.Block);
            return result;
        }

        /// <summary>
        /// Validates a single section's text content.
        /// </summary>
        public static ModerationResult ValidateSection(int sectionId, string text)
        {
            var result = new ModerationResult();

            if (string.IsNullOrWhiteSpace(text))
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "SECT_001", message = $"Seção {sectionId} está vazia", location = $"sections.{sectionId}.text" });

            if (text != null && text.Length > 10000)
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "SECT_002", message = $"Seção {sectionId} muito longa (>10.000 caracteres)", location = $"sections.{sectionId}.text" });

            CheckPatterns(text ?? "", $"sections.{sectionId}.text", result);

            result.passed = !result.issues.Exists(i => i.severity == Severity.Block);
            return result;
        }

        /// <summary>
        /// Validates the entire story structure.
        /// </summary>
        public static ModerationResult ValidateAllSections(Dictionary<int, string> sectionsById)
        {
            var result = new ModerationResult();

            if (sectionsById == null || sectionsById.Count == 0)
            {
                result.issues.Add(new ModerationIssue { severity = Severity.Block, code = "SECT_010", message = "História sem seções", location = "sections" });
                result.passed = false;
                return result;
            }

            if (sectionsById.Count < 5)
                result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = "SECT_011", message = $"História com apenas {sectionsById.Count} seções (mínimo recomendado: 5)", location = "sections" });

            foreach (var kvp in sectionsById)
            {
                var sectionResult = ValidateSection(kvp.Key, kvp.Value);
                result.issues.AddRange(sectionResult.issues);
            }

            result.passed = !result.issues.Exists(i => i.severity == Severity.Block);
            return result;
        }

        private static void CheckPatterns(string text, string location, ModerationResult result)
        {
            if (string.IsNullOrEmpty(text)) return;

            foreach (var (pattern, code, description) in BlockedPatterns)
            {
                if (pattern.IsMatch(text))
                    result.issues.Add(new ModerationIssue { severity = Severity.Block, code = code, message = description, location = location });
            }

            foreach (var (pattern, code, description) in WarningPatterns)
            {
                if (pattern.IsMatch(text))
                    result.issues.Add(new ModerationIssue { severity = Severity.Warning, code = code, message = description, location = location });
            }
        }
    }
}
