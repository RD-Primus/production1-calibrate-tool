using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PH_08_Test
{
    public sealed class QcPayloadV2
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = 2;
        [JsonPropertyName("update_mode")] public bool UpdateMode { get; set; } = false;
        [JsonPropertyName("model_code")] public string ModelCode { get; set; } = "PH-08";
        [JsonPropertyName("lot_number")] public string LotNumber { get; set; } = "";
        [JsonPropertyName("ecn_number")] public string EcnNumber { get; set; } = "";
        [JsonPropertyName("serial_number")] public string SerialNumber { get; set; } = "";
        [JsonPropertyName("station_name")] public string StationName { get; set; } = "QC-STATION-01";
        [JsonPropertyName("operator_username")] public string OperatorUsername { get; set; } = "";
        [JsonPropertyName("start_time_inspection_datetime")] public string StartTime { get; set; } = "";
        [JsonPropertyName("inspection_datetime")] public string InspectionTime { get; set; } = "";
        [JsonPropertyName("template_name")] public string TemplateName { get; set; } = "PH-08 QC Inspection.xlsx";
        [JsonPropertyName("description")] public string Description { get; set; } = "PH-08 I/O MAPPER Functional and Communication Inspection";
        [JsonPropertyName("firmware")] public List<QcFirmware> Firmware { get; set; } = new();
        [JsonPropertyName("powersupply")] public string PowerSupply { get; set; } = "9-30 VDC";
        [JsonPropertyName("equipment_codes")] public List<QcEquipment> EquipmentCodes { get; set; } = new();
        [JsonIgnore] public List<QcEquipment> EquipmentCode { get => EquipmentCodes; set => EquipmentCodes = value; }
        [JsonPropertyName("approve")] public QcApprove Approve { get; set; } = new();
        [JsonPropertyName("remark")] public string Remark { get; set; } = "";
        [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
        [JsonPropertyName("inspection_status")] public string InspectionStatus { get; set; } = "final";
        [JsonPropertyName("product_name")] public string ProductName { get; set; } = "I/O MAPPER";
        [JsonPropertyName("report_serial_number")] public string ReportSerialNumber { get; set; } = "";
        [JsonPropertyName("jig_name")] public string JigName { get; set; } = "PH08-FUNCTIONAL-JIG-01";
        [JsonPropertyName("report_date")] public string ReportDate { get; set; } = "";
        [JsonPropertyName("overall_result")] public string OverallResult { get; set; } = "PASS";

        [JsonPropertyName("lot_size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? LotSize { get; set; }

        [JsonPropertyName("acceptance_checks")] public List<QcAcceptanceCheck> AcceptanceChecks { get; set; } = new();
        [JsonPropertyName("items")] public List<QcItem> Items { get; set; } = new();
        [JsonPropertyName("report_mapping")] public QcReportMappingConfig ReportMapping { get; set; } = new();
        [JsonPropertyName("images")] public List<object> Images { get; set; } = new();
    }

    public sealed class QcFirmware
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("version")] public string Version { get; set; } = "";
    }

    public sealed class QcEquipment
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("series")] public string Series { get; set; } = "";
        [JsonPropertyName("equipment_id")] public string EquipmentId { get; set; } = "";

        [JsonPropertyName("expire_date")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExpireDate { get; set; }
    }

    public sealed class QcSignature
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("datetime")] public string? Datetime { get; set; }
    }

    public sealed class QcApprove
    {
        [JsonPropertyName("tester")] public QcSignature Tester { get; set; } = new();
        [JsonPropertyName("confirm")] public QcSignature Confirm { get; set; } = new();
        [JsonPropertyName("approver")] public QcSignature Approver { get; set; } = new();
    }

    public sealed class QcOffset
    {
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
    }

    public sealed class QcCheckReportMapping
    {
        [JsonPropertyName("mapping_strategy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MappingStrategy { get; set; } = "worksheet_search";

        [JsonPropertyName("target_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TargetType { get; set; } = "cell";

        [JsonPropertyName("search_key")] public string SearchKey { get; set; } = "";

        [JsonPropertyName("match_mode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MatchMode { get; set; }

        [JsonPropertyName("offset")] public QcOffset Offset { get; set; } = new();
        [JsonPropertyName("value_field")] public string ValueField { get; set; } = "checked";
        [JsonPropertyName("write_mode")] public string WriteMode { get; set; } = "value_map";
        [JsonPropertyName("value_map")] public Dictionary<string, string> ValueMap { get; set; } = new();
    }

    public sealed class QcAcceptanceCheck
    {
        [JsonPropertyName("check_code")] public string CheckCode { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("checked")] public bool Checked { get; set; } = true;
        [JsonPropertyName("report_mapping")] public List<QcCheckReportMapping> ReportMapping { get; set; } = new();
    }

    public sealed class QcItem
    {
        [JsonPropertyName("item_code")] public string ItemCode { get; set; } = "";
        [JsonPropertyName("test_name")] public string TestName { get; set; } = "";
        [JsonPropertyName("required")] public bool Required { get; set; } = true;
        [JsonPropertyName("measured_text")] public string MeasuredText { get; set; } = "";

        [JsonPropertyName("measured_value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MeasuredValue { get; set; }

        [JsonPropertyName("minValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MinValue { get; set; }

        [JsonPropertyName("maxValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MaxValue { get; set; }

        [JsonPropertyName("unit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Unit { get; set; }

        [JsonPropertyName("result")] public string Result { get; set; } = "";
        [JsonPropertyName("report_mapping")] public List<QcItemReportMapping> ReportMapping { get; set; } = new();
    }

    public sealed class QcItemReportMapping
    {
        [JsonPropertyName("target_type")] public string TargetType { get; set; } = "table_cell";
        [JsonPropertyName("table_key")] public string TableKey { get; set; } = "inspection_items";
        [JsonPropertyName("column_path")] public List<string> ColumnPath { get; set; } = new();

        [JsonPropertyName("column_anchor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnAnchor { get; set; } = "merge_top_left";

        [JsonPropertyName("offset")] public QcOffset Offset { get; set; } = new();
        [JsonPropertyName("value_field")] public string ValueField { get; set; } = "result";
        [JsonPropertyName("write_mode")] public string WriteMode { get; set; } = "value_map";

        [JsonPropertyName("value_map")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? ValueMap { get; set; }

        [JsonPropertyName("number_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NumberFormat { get; set; }
    }

    public sealed class QcSearchOptions
    {
        [JsonPropertyName("ignore_case")] public bool IgnoreCase { get; set; } = true;
        [JsonPropertyName("normalize_whitespace")] public bool NormalizeWhitespace { get; set; } = true;
        [JsonPropertyName("normalize_numeric_headers")] public bool NormalizeNumericHeaders { get; set; } = true;
        [JsonPropertyName("on_missing")] public string OnMissing { get; set; } = "skip";
        [JsonPropertyName("on_multiple")] public string OnMultiple { get; set; } = "error";
    }

    public sealed class QcReportField
    {
        [JsonPropertyName("target_type")] public string TargetType { get; set; } = "cell";
        [JsonPropertyName("search_key")] public string SearchKey { get; set; } = "";

        [JsonPropertyName("match_mode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MatchMode { get; set; }

        [JsonPropertyName("offset")] public QcOffset Offset { get; set; } = new();
        [JsonPropertyName("value_field")] public string ValueField { get; set; } = "";
        [JsonPropertyName("write_mode")] public string WriteMode { get; set; } = "value";

        [JsonPropertyName("text_template")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextTemplate { get; set; }

        [JsonPropertyName("entry_template")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EntryTemplate { get; set; }

        [JsonPropertyName("separator")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Separator { get; set; }

        [JsonPropertyName("prefix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Prefix { get; set; }

        [JsonPropertyName("number_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NumberFormat { get; set; }
    }

    public sealed class QcAnchor
    {
        [JsonPropertyName("search_key")] public string SearchKey { get; set; } = "No";
        [JsonPropertyName("match_mode")] public string MatchMode { get; set; } = "exact";
    }

    public sealed class QcRecordLayout
    {
        [JsonPropertyName("rows_per_record")] public int RowsPerRecord { get; set; } = 1;
    }

    public sealed class QcDataBlock
    {
        [JsonPropertyName("block_id")] public string BlockId { get; set; } = "block_1";
        [JsonPropertyName("start_row_offset")] public int StartRowOffset { get; set; } = 2;
        [JsonPropertyName("end_row_offset")] public int EndRowOffset { get; set; } = 29;
    }

    public sealed class QcRowKey
    {
        [JsonPropertyName("column_path")] public List<string> ColumnPath { get; set; } = new() { "Inspection", "S/N" };
        [JsonPropertyName("value_field")] public string ValueField { get; set; } = "serial_number";
        [JsonPropertyName("match_mode")] public string MatchMode { get; set; } = "exact";
    }

    public sealed class QcRowPolicy
    {
        [JsonPropertyName("on_existing")] public string OnExisting { get; set; } = "update";
        [JsonPropertyName("on_missing")] public string OnMissing { get; set; } = "first_empty_record";
        [JsonPropertyName("on_full")] public string OnFull { get; set; } = "error";
    }

    public sealed class QcPreserveColumn
    {
        [JsonPropertyName("column_path")] public List<string> ColumnPath { get; set; } = new() { "No" };
    }

    public sealed class QcTableRowField
    {
        [JsonPropertyName("target_type")] public string TargetType { get; set; } = "table_cell";
        [JsonPropertyName("table_key")] public string TableKey { get; set; } = "inspection_items";
        [JsonPropertyName("column_path")] public List<string> ColumnPath { get; set; } = new();

        [JsonPropertyName("column_anchor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ColumnAnchor { get; set; } = "merge_top_left";

        [JsonPropertyName("offset")] public QcOffset Offset { get; set; } = new();
        [JsonPropertyName("value_field")] public string ValueField { get; set; } = "";
        [JsonPropertyName("write_mode")] public string WriteMode { get; set; } = "value";

        [JsonPropertyName("value_map")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? ValueMap { get; set; }
    }

    public sealed class QcReportTable
    {
        [JsonPropertyName("table_key")] public string TableKey { get; set; } = "inspection_items";
        [JsonPropertyName("anchor")] public QcAnchor Anchor { get; set; } = new();
        [JsonPropertyName("header_row_offsets")] public List<int> HeaderRowOffsets { get; set; } = new() { 0, 1 };
        [JsonPropertyName("column_span")] public int ColumnSpan { get; set; } = 15;
        [JsonPropertyName("record_layout")] public QcRecordLayout RecordLayout { get; set; } = new();
        [JsonPropertyName("data_blocks")] public List<QcDataBlock> DataBlocks { get; set; } = new();
        [JsonPropertyName("reference_row_offsets")] public List<int> ReferenceRowOffsets { get; set; } = new() { 0, 1 };
        [JsonPropertyName("row_key")] public QcRowKey RowKey { get; set; } = new();
        [JsonPropertyName("row_policy")] public QcRowPolicy RowPolicy { get; set; } = new();
        [JsonPropertyName("preserve_columns")] public List<QcPreserveColumn> PreserveColumns { get; set; } = new();
        [JsonPropertyName("row_fields")] public List<QcTableRowField> RowFields { get; set; } = new();
    }

    public sealed class QcFormulaPolicy
    {
        [JsonPropertyName("on_write")] public string OnWrite { get; set; } = "error";
        [JsonPropertyName("on_clear")] public string OnClear { get; set; } = "preserve";
        [JsonPropertyName("recalculate")] public bool Recalculate { get; set; } = true;
    }

    public sealed class QcResultRules
    {
        [JsonPropertyName("source")] public string Source { get; set; } = "items[].result";
        [JsonPropertyName("allowed_item_results")] public List<string> AllowedItemResults { get; set; } = new() { "PASS", "FAIL", "PENDING", "SKIP" };
        [JsonPropertyName("required_items")] public string RequiredItems { get; set; } = "all_required";
        [JsonPropertyName("fail_when")] public string FailWhen { get; set; } = "any_fail";
        [JsonPropertyName("pass_when")] public string PassWhen { get; set; } = "all_pass";
        [JsonPropertyName("on_required_skip")] public string OnRequiredSkip { get; set; } = "PENDING";
        [JsonPropertyName("otherwise")] public string Otherwise { get; set; } = "PENDING";
        [JsonPropertyName("validate_numeric_limits")] public bool ValidateNumericLimits { get; set; } = true;
        [JsonPropertyName("on_missing_numeric_limits")] public string OnMissingNumericLimits { get; set; } = "use_supplied_result";
    }

    public sealed class QcSummaryField
    {
        [JsonPropertyName("metric")] public string Metric { get; set; } = "";
        [JsonPropertyName("search_key")] public string SearchKey { get; set; } = "";
        [JsonPropertyName("match_mode")] public string MatchMode { get; set; } = "exact";
        [JsonPropertyName("offset")] public QcOffset Offset { get; set; } = new() { X = 0, Y = 1 };
        [JsonPropertyName("write_mode")] public string WriteMode { get; set; } = "preserve_formula";
    }

    public sealed class QcSummaryConfig
    {
        [JsonPropertyName("source")] public string Source { get; set; } = "template_formula";
        [JsonPropertyName("fields")] public List<QcSummaryField> Fields { get; set; } = new();
    }

    public sealed class QcDataRowRange
    {
        [JsonPropertyName("from")] public int From { get; set; } = 7;
        [JsonPropertyName("to")] public int To { get; set; } = 52;
    }

    public sealed class QcHeaderRows
    {
        [JsonPropertyName("from")] public int From { get; set; } = 1;
        [JsonPropertyName("to")] public int To { get; set; } = 6;
    }

    public sealed class QcPageLayout
    {
        [JsonPropertyName("freeze_panes")] public bool FreezePanes { get; set; } = true;
        [JsonPropertyName("header_mode")] public string HeaderMode { get; set; } = "REPEAT";
        [JsonPropertyName("header_rows")] public QcHeaderRows HeaderRows { get; set; } = new();
    }

    public sealed class QcResultMapping
    {
        [JsonPropertyName("pass_column")] public string PassColumn { get; set; } = "M";
        [JsonPropertyName("fail_column")] public string FailColumn { get; set; } = "N";
        [JsonPropertyName("mark")] public string Mark { get; set; } = "✔";
        [JsonPropertyName("source_field")] public string SourceField { get; set; } = "overall_result";
        [JsonPropertyName("unselected_value")] public string UnselectedValue { get; set; } = "";
    }

    public sealed class QcHeaderMapping
    {
        [JsonPropertyName("field")] public string Field { get; set; } = "";
        [JsonPropertyName("target_cell")] public string TargetCell { get; set; } = "";
        [JsonPropertyName("write_mode")] public string WriteMode { get; set; } = "value_only";

        [JsonPropertyName("prefix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Prefix { get; set; }

        [JsonPropertyName("fallback")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Fallback { get; set; }

        [JsonPropertyName("required")] public bool Required { get; set; } = false;
    }

    public sealed class QcItemMapping
    {
        [JsonPropertyName("item_code")] public string ItemCode { get; set; } = "";
        [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
        [JsonPropertyName("target_column")] public string TargetColumn { get; set; } = "";
        [JsonPropertyName("row_offset")] public int RowOffset { get; set; } = 0;
        [JsonPropertyName("source_field")] public string SourceField { get; set; } = "result";

        [JsonPropertyName("fallback_field")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FallbackField { get; set; }

        [JsonPropertyName("empty_value")] public string EmptyValue { get; set; } = "";

        [JsonPropertyName("number_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NumberFormat { get; set; }

        [JsonPropertyName("accepted_item_codes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AcceptedItemCodes { get; set; }

        [JsonPropertyName("required")] public bool Required { get; set; } = true;

        [JsonPropertyName("value_map")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? ValueMap { get; set; }
    }

    public sealed class QcServerTemplateSettings
    {
        [JsonPropertyName("data_rows")] public List<QcDataRowRange> DataRows { get; set; } = new();
        [JsonPropertyName("max_records_per_sheet")] public int MaxRecordsPerSheet { get; set; } = 46;
        [JsonPropertyName("overflow_strategy")] public string OverflowStrategy { get; set; } = "NEW_SHEET";
        [JsonPropertyName("page_layout")] public QcPageLayout PageLayout { get; set; } = new();
        [JsonPropertyName("sequence_column")] public string SequenceColumn { get; set; } = "A";
        [JsonPropertyName("serial_number_column")] public string SerialNumberColumn { get; set; } = "P";
        [JsonPropertyName("result_mapping")] public QcResultMapping ResultMapping { get; set; } = new();
        [JsonPropertyName("group_by")] public List<string> GroupBy { get; set; } = new() { "model_code", "lot_number", "inspection_date" };
        [JsonPropertyName("sort_by")] public List<string> SortBy { get; set; } = new() { "inspection_datetime", "id" };
        [JsonPropertyName("timezone")] public string Timezone { get; set; } = "Asia/Bangkok";
        [JsonPropertyName("file_name_pattern")] public string FileNamePattern { get; set; } = "{inspection_date}_{lot_size}_{model_code}_{lot_number}_{document_code}-R{revision}.xlsx";
        [JsonPropertyName("header_mappings")] public List<QcHeaderMapping> HeaderMappings { get; set; } = new();
        [JsonPropertyName("item_mappings")] public List<QcItemMapping> ItemMappings { get; set; } = new();
    }

    public sealed class QcReportMappingConfig
    {
        [JsonPropertyName("template_id")] public string TemplateId { get; set; } = "PH08_F_PD07_R7";

        [JsonPropertyName("template_source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TemplateSource { get; set; } = "PH-08 QC Inspection.xlsx";

        [JsonPropertyName("sheet_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SheetName { get; set; } = "Sheet1";

        [JsonPropertyName("mapping_strategy")] public string MappingStrategy { get; set; } = "server_template_settings";
        [JsonPropertyName("output_mode")] public string OutputMode { get; set; } = "server_template_settings";
        [JsonPropertyName("data_scope")] public string DataScope { get; set; } = "selected_inspections";

        [JsonPropertyName("revision")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Revision { get; set; } = "1.0.0";

        [JsonPropertyName("server_template_settings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QcServerTemplateSettings? ServerTemplateSettings { get; set; }

        [JsonPropertyName("clear_old_data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ClearOldData { get; set; }

        [JsonPropertyName("clear_mode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClearMode { get; set; }

        [JsonPropertyName("require_complete_for_final")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? RequireCompleteForFinal { get; set; }

        [JsonPropertyName("skip_missing_values")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SkipMissingValues { get; set; }

        [JsonPropertyName("search_options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QcSearchOptions? SearchOptions { get; set; }

        [JsonPropertyName("fields")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<QcReportField>? Fields { get; set; }

        [JsonPropertyName("tables")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<QcReportTable>? Tables { get; set; }

        [JsonPropertyName("formula_policy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QcFormulaPolicy? FormulaPolicy { get; set; }

        [JsonPropertyName("result_rules")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QcResultRules? ResultRules { get; set; }

        [JsonPropertyName("summary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QcSummaryConfig? Summary { get; set; }
    }
}
