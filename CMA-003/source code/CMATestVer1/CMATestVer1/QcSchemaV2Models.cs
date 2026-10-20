using Newtonsoft.Json;
using System.Collections.Generic;

namespace CMATestVer1
{
    public sealed class QcOffset
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }

    public sealed class QcCheckReportMapping
    {
        [JsonProperty("mapping_strategy", NullValueHandling = NullValueHandling.Ignore)]
        public string? MappingStrategy { get; set; } = "worksheet_search";

        [JsonProperty("search_key")]
        public string SearchKey { get; set; } = string.Empty;

        [JsonProperty("match_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string? MatchMode { get; set; }

        [JsonProperty("offset")]
        public QcOffset Offset { get; set; } = new QcOffset();

        [JsonProperty("value_field")]
        public string ValueField { get; set; } = "checked";

        [JsonProperty("write_mode")]
        public string WriteMode { get; set; } = "value_map";

        [JsonProperty("value_map")]
        public Dictionary<string, string> ValueMap { get; set; } = new Dictionary<string, string>
        {
            { "true", "☑" },
            { "false", "☐" }
        };
    }

    public sealed class QcAcceptanceCheck
    {
        [JsonProperty("check_code")]
        public string CheckCode { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("checked")]
        public bool Checked { get; set; } = true;

        [JsonProperty("report_mapping")]
        public List<QcCheckReportMapping> ReportMapping { get; set; } = new List<QcCheckReportMapping>();
    }

    public sealed class QcItemReportMapping
    {
        [JsonProperty("target_type")]
        public string TargetType { get; set; } = "table_cell";

        [JsonProperty("table_key")]
        public string TableKey { get; set; } = "inspection_items";

        [JsonProperty("column_path")]
        public List<string> ColumnPath { get; set; } = new List<string>();

        [JsonProperty("column_anchor", NullValueHandling = NullValueHandling.Ignore)]
        public string? ColumnAnchor { get; set; }

        [JsonProperty("offset")]
        public QcOffset Offset { get; set; } = new QcOffset();

        [JsonProperty("value_field")]
        public string ValueField { get; set; } = "measured_text";

        [JsonProperty("write_mode")]
        public string WriteMode { get; set; } = "value";

        [JsonProperty("value_map", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? ValueMap { get; set; }

        [JsonProperty("number_format", NullValueHandling = NullValueHandling.Ignore)]
        public string? NumberFormat { get; set; }
    }

    public sealed class QcSearchOptions
    {
        [JsonProperty("ignore_case")]
        public bool IgnoreCase { get; set; } = true;

        [JsonProperty("normalize_whitespace")]
        public bool NormalizeWhitespace { get; set; } = true;

        [JsonProperty("normalize_numeric_headers")]
        public bool NormalizeNumericHeaders { get; set; } = true;

        [JsonProperty("on_missing")]
        public string OnMissing { get; set; } = "skip";

        [JsonProperty("on_multiple")]
        public string OnMultiple { get; set; } = "error";
    }

    public sealed class QcReportField
    {
        [JsonProperty("search_key")]
        public string SearchKey { get; set; } = string.Empty;

        [JsonProperty("match_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string? MatchMode { get; set; }

        [JsonProperty("offset")]
        public QcOffset Offset { get; set; } = new QcOffset();

        [JsonProperty("value_field")]
        public string ValueField { get; set; } = string.Empty;

        [JsonProperty("write_mode")]
        public string WriteMode { get; set; } = "value";

        [JsonProperty("text_template", NullValueHandling = NullValueHandling.Ignore)]
        public string? TextTemplate { get; set; }

        [JsonProperty("entry_template", NullValueHandling = NullValueHandling.Ignore)]
        public string? EntryTemplate { get; set; }

        [JsonProperty("separator", NullValueHandling = NullValueHandling.Ignore)]
        public string? Separator { get; set; }

        [JsonProperty("prefix", NullValueHandling = NullValueHandling.Ignore)]
        public string? Prefix { get; set; }

        [JsonProperty("number_format", NullValueHandling = NullValueHandling.Ignore)]
        public string? NumberFormat { get; set; }
    }

    public sealed class QcAnchor
    {
        [JsonProperty("search_key")]
        public string SearchKey { get; set; } = "No";

        [JsonProperty("match_mode")]
        public string MatchMode { get; set; } = "starts_with";
    }

    public sealed class QcRecordLayout
    {
        [JsonProperty("rows_per_record")]
        public int RowsPerRecord { get; set; } = 1;
    }

    public sealed class QcDataBlock
    {
        [JsonProperty("block_id")]
        public string BlockId { get; set; } = "block_1";

        [JsonProperty("start_row_offset")]
        public int StartRowOffset { get; set; } = 2;

        [JsonProperty("end_row_offset")]
        public int EndRowOffset { get; set; } = 30;
    }

    public sealed class QcRowKey
    {
        [JsonProperty("column_path")]
        public List<string> ColumnPath { get; set; } = new List<string> { "Inspection", "S/N" };

        [JsonProperty("value_field")]
        public string ValueField { get; set; } = "serial_number";

        [JsonProperty("match_mode")]
        public string MatchMode { get; set; } = "exact";
    }

    public sealed class QcRowPolicy
    {
        [JsonProperty("on_existing")]
        public string OnExisting { get; set; } = "update";

        [JsonProperty("on_missing")]
        public string OnMissing { get; set; } = "first_empty_record";

        [JsonProperty("on_full")]
        public string OnFull { get; set; } = "error";
    }

    public sealed class QcPreserveColumn
    {
        [JsonProperty("column_path")]
        public List<string> ColumnPath { get; set; } = new List<string> { "No." };
    }

    public sealed class QcRowField
    {
        [JsonProperty("target_type")]
        public string TargetType { get; set; } = "table_cell";

        [JsonProperty("table_key")]
        public string TableKey { get; set; } = "inspection_items";

        [JsonProperty("column_path")]
        public List<string> ColumnPath { get; set; } = new List<string>();

        [JsonProperty("column_anchor", NullValueHandling = NullValueHandling.Ignore)]
        public string? ColumnAnchor { get; set; }

        [JsonProperty("offset")]
        public QcOffset Offset { get; set; } = new QcOffset();

        [JsonProperty("value_field")]
        public string ValueField { get; set; } = string.Empty;

        [JsonProperty("write_mode")]
        public string WriteMode { get; set; } = "value";

        [JsonProperty("value_map", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? ValueMap { get; set; }
    }

    public sealed class QcReportTable
    {
        [JsonProperty("table_key")]
        public string TableKey { get; set; } = "inspection_items";

        [JsonProperty("anchor")]
        public QcAnchor Anchor { get; set; } = new QcAnchor();

        [JsonProperty("header_row_offsets")]
        public List<int> HeaderRowOffsets { get; set; } = new List<int> { 0, 1 };

        [JsonProperty("column_span")]
        public int ColumnSpan { get; set; } = 28;

        [JsonProperty("record_layout")]
        public QcRecordLayout RecordLayout { get; set; } = new QcRecordLayout();

        [JsonProperty("data_blocks")]
        public List<QcDataBlock> DataBlocks { get; set; } = new List<QcDataBlock>();

        [JsonProperty("reference_row_offsets")]
        public List<int> ReferenceRowOffsets { get; set; } = new List<int> { 0, 1 };

        [JsonProperty("row_key")]
        public QcRowKey RowKey { get; set; } = new QcRowKey();

        [JsonProperty("row_policy")]
        public QcRowPolicy RowPolicy { get; set; } = new QcRowPolicy();

        [JsonProperty("preserve_columns")]
        public List<QcPreserveColumn> PreserveColumns { get; set; } = new List<QcPreserveColumn>();

        [JsonProperty("row_fields")]
        public List<QcRowField> RowFields { get; set; } = new List<QcRowField>();
    }

    public sealed class QcFormulaPolicy
    {
        [JsonProperty("on_write")]
        public string OnWrite { get; set; } = "error";

        [JsonProperty("on_clear")]
        public string OnClear { get; set; } = "preserve";

        [JsonProperty("recalculate")]
        public bool Recalculate { get; set; } = true;
    }

    public sealed class QcResultRules
    {
        [JsonProperty("source")]
        public string Source { get; set; } = "items[].result";

        [JsonProperty("allowed_item_results")]
        public List<string> AllowedItemResults { get; set; } = new List<string> { "PASS", "FAIL", "PENDING", "SKIP" };

        [JsonProperty("required_items")]
        public string RequiredItems { get; set; } = "all_required";

        [JsonProperty("fail_when")]
        public string FailWhen { get; set; } = "any_fail";

        [JsonProperty("pass_when")]
        public string PassWhen { get; set; } = "all_pass";

        [JsonProperty("on_required_skip")]
        public string OnRequiredSkip { get; set; } = "PENDING";

        [JsonProperty("otherwise")]
        public string Otherwise { get; set; } = "PENDING";

        [JsonProperty("validate_numeric_limits")]
        public bool ValidateNumericLimits { get; set; } = true;

        [JsonProperty("on_missing_numeric_limits")]
        public string OnMissingNumericLimits { get; set; } = "use_supplied_result";
    }

    public sealed class QcSummaryField
    {
        [JsonProperty("metric")]
        public string Metric { get; set; } = string.Empty;

        [JsonProperty("search_key")]
        public string SearchKey { get; set; } = string.Empty;

        [JsonProperty("match_mode")]
        public string MatchMode { get; set; } = "exact";

        [JsonProperty("offset")]
        public QcOffset Offset { get; set; } = new QcOffset { X = 0, Y = 1 };

        [JsonProperty("write_mode")]
        public string WriteMode { get; set; } = "preserve_formula";
    }

    public sealed class QcSummaryConfig
    {
        [JsonProperty("source")]
        public string Source { get; set; } = "template_formula";

        [JsonProperty("fields")]
        public List<QcSummaryField> Fields { get; set; } = new List<QcSummaryField>();
    }

    public sealed class QcDataRowRange
    {
        [JsonProperty("from")]
        public int From { get; set; } = 9;

        [JsonProperty("to")]
        public int To { get; set; } = 34;
    }

    public sealed class QcHeaderRows
    {
        [JsonProperty("from")]
        public int From { get; set; } = 1;

        [JsonProperty("to")]
        public int To { get; set; } = 8;
    }

    public sealed class QcPageLayout
    {
        [JsonProperty("freeze_panes")]
        public bool FreezePanes { get; set; } = true;

        [JsonProperty("header_mode")]
        public string HeaderMode { get; set; } = "REPEAT";

        [JsonProperty("header_rows")]
        public QcHeaderRows HeaderRows { get; set; } = new QcHeaderRows();
    }

    public sealed class QcResultMapping
    {
        [JsonProperty("pass_column")]
        public string PassColumn { get; set; } = "Z";

        [JsonProperty("fail_column")]
        public string FailColumn { get; set; } = "AA";

        [JsonProperty("mark")]
        public string Mark { get; set; } = "✔";

        [JsonProperty("source_field")]
        public string SourceField { get; set; } = "overall_result";

        [JsonProperty("unselected_value")]
        public string UnselectedValue { get; set; } = "";
    }

    public sealed class QcHeaderMapping
    {
        [JsonProperty("field")]
        public string Field { get; set; } = string.Empty;

        [JsonProperty("target_cell")]
        public string TargetCell { get; set; } = string.Empty;

        [JsonProperty("write_mode")]
        public string WriteMode { get; set; } = "replace_full_text";

        [JsonProperty("prefix", NullValueHandling = NullValueHandling.Ignore)]
        public string? Prefix { get; set; }

        [JsonProperty("fallback", NullValueHandling = NullValueHandling.Ignore)]
        public string? Fallback { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; } = false;
    }

    public sealed class QcItemMapping
    {
        [JsonProperty("item_code")]
        public string ItemCode { get; set; } = string.Empty;

        [JsonProperty("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("target_column")]
        public string TargetColumn { get; set; } = string.Empty;

        [JsonProperty("row_offset")]
        public int RowOffset { get; set; } = 0;

        [JsonProperty("source_field")]
        public string SourceField { get; set; } = "result";

        [JsonProperty("fallback_field", NullValueHandling = NullValueHandling.Include)]
        public string? FallbackField { get; set; }

        [JsonProperty("empty_value")]
        public string EmptyValue { get; set; } = "";

        [JsonProperty("number_format", NullValueHandling = NullValueHandling.Ignore)]
        public string? NumberFormat { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; } = true;

        [JsonProperty("value_map", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? ValueMap { get; set; }
    }

    public sealed class QcServerTemplateSettings
    {
        [JsonProperty("data_rows")]
        public List<QcDataRowRange> DataRows { get; set; } = new List<QcDataRowRange>();

        [JsonProperty("max_records_per_sheet")]
        public int MaxRecordsPerSheet { get; set; } = 26;

        [JsonProperty("overflow_strategy")]
        public string OverflowStrategy { get; set; } = "NEW_SHEET";

        [JsonProperty("page_layout")]
        public QcPageLayout PageLayout { get; set; } = new QcPageLayout();

        [JsonProperty("sequence_column")]
        public string SequenceColumn { get; set; } = "A";

        [JsonProperty("serial_number_column")]
        public string SerialNumberColumn { get; set; } = "AB";

        [JsonProperty("result_mapping")]
        public QcResultMapping ResultMapping { get; set; } = new QcResultMapping();

        [JsonProperty("group_by")]
        public List<string> GroupBy { get; set; } = new List<string> { "model_code", "lot_number", "inspection_date" };

        [JsonProperty("sort_by")]
        public List<string> SortBy { get; set; } = new List<string> { "inspection_datetime", "id" };

        [JsonProperty("timezone")]
        public string Timezone { get; set; } = "Asia/Bangkok";

        [JsonProperty("file_name_pattern")]
        public string FileNamePattern { get; set; } = "{inspection_date}_{lot_size}_{model_code}_{lot_number}_{document_code}-R{revision}.xlsx";

        [JsonProperty("header_mappings")]
        public List<QcHeaderMapping> HeaderMappings { get; set; } = new List<QcHeaderMapping>();

        [JsonProperty("item_mappings")]
        public List<QcItemMapping> ItemMappings { get; set; } = new List<QcItemMapping>();
    }

    public sealed class QcReportMappingConfig
    {
        [JsonProperty("template_id")]
        public string TemplateId { get; set; } = "5";

        [JsonProperty("template_source", NullValueHandling = NullValueHandling.Ignore)]
        public string? TemplateSource { get; set; } = "CMA-003 QC Inspection.xlsx";

        [JsonProperty("sheet_name", NullValueHandling = NullValueHandling.Include)]
        public string? SheetName { get; set; } = "Sheet1";

        [JsonProperty("mapping_strategy")]
        public string MappingStrategy { get; set; } = "server_template_settings";

        [JsonProperty("output_mode")]
        public string OutputMode { get; set; } = "server_template_settings";

        [JsonProperty("data_scope")]
        public string DataScope { get; set; } = "selected_inspections";

        [JsonProperty("revision", NullValueHandling = NullValueHandling.Ignore)]
        public string Revision { get; set; } = "1.0.0";

        [JsonProperty("server_template_settings", NullValueHandling = NullValueHandling.Ignore)]
        public QcServerTemplateSettings? ServerTemplateSettings { get; set; }

        [JsonProperty("clear_old_data", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ClearOldData { get; set; }

        [JsonProperty("clear_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string? ClearMode { get; set; }

        [JsonProperty("require_complete_for_final", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RequireCompleteForFinal { get; set; }

        [JsonProperty("skip_missing_values", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SkipMissingValues { get; set; }

        [JsonProperty("search_options", NullValueHandling = NullValueHandling.Ignore)]
        public QcSearchOptions? SearchOptions { get; set; }

        [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
        public List<QcReportField>? Fields { get; set; }

        [JsonProperty("tables", NullValueHandling = NullValueHandling.Ignore)]
        public List<QcReportTable>? Tables { get; set; }

        [JsonProperty("formula_policy", NullValueHandling = NullValueHandling.Ignore)]
        public QcFormulaPolicy? FormulaPolicy { get; set; }

        [JsonProperty("result_rules", NullValueHandling = NullValueHandling.Ignore)]
        public QcResultRules? ResultRules { get; set; }

        [JsonProperty("summary", NullValueHandling = NullValueHandling.Ignore)]
        public QcSummaryConfig? Summary { get; set; }
    }
}
