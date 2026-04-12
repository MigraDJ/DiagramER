using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DiagramER.Components.Pages;

public partial class Diagramer
{
    private class TableModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Table";
        public double X { get; set; } = 50;
        public double Y { get; set; } = 50;
        public double Width { get; set; } = 600;
        public double Height { get; set; } = 400;
        public List<ColumnModel> Rows { get; set; } = new();
    }

    private class ColumnModel
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "";
        public int? Length { get; set; }
        public string DefaultValue { get; set; } = "";
        public string Null { get; set; } = "";
        public string PrimaryKey { get; set; } = "";
    }

    private class RelationModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FromTableId { get; set; } = "";
        public int FromRowIndex { get; set; } = 0;
        public string FromColumn { get; set; } = "name";
        public string ToTableId { get; set; } = "";
        public int ToRowIndex { get; set; } = 0;
        public string ToColumn { get; set; } = "name";
        public string Name { get; set; } = "New Relation";
        public bool HasCustomName { get; set; } = false;
        public double? ManualRouteX { get; set; }
    }

    private class TextLabelModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = "New label";
        public double X { get; set; } = 420;
        public double Y { get; set; } = 120;
    }

    private class DrawingRelationModel
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double CurrentX { get; set; }
        public double CurrentY { get; set; }
        public string FromTableId { get; set; } = "";
        public int FromRowIndex { get; set; } = 0;
    }

    private List<TableModel> Tables = new();
    private List<RelationModel> Relations = new();
    private List<TextLabelModel> TextLabels = new();
    private string? EditingTableId = null;
    private string? EditingTextLabelId = null;
    private string DraggedItemType = "";
    private DrawingRelationModel? DrawingRelation = null;
    private TableModel? DraggingTable = null;
    private TextLabelModel? DraggingTextLabel = null;
    private double DragOffsetX = 0;
    private double DragOffsetY = 0;
    private string JsonOutput = "{}";
    private int SelectedRowIndex = -1;
    private string SelectedTableId = "";
    private string? SelectedRelationId = null;
    private string? EditingRelationId = null;
    private bool _relationClickedThisFrame = false;
    private string? DraggingWaypointRelationId = null;
    private double WaypointDragStartX = 0;
    private double WaypointStartRouteX = 0;
    private bool IsPanningCanvas = false;
    private double CanvasZoom = 1;
    private double CanvasOffsetX = 0;
    private double CanvasOffsetY = 0;
    private double PanStartClientX = 0;
    private double PanStartClientY = 0;
    private double PanStartOffsetX = 0;
    private double PanStartOffsetY = 0;
    private TableModel? ResizingTable = null;
    private double ResizeStartX = 0;
    private double ResizeStartY = 0;
    private double ResizeStartWidth = 0;
    private double ResizeStartHeight = 0;
    private ElementReference fileInput;

    // Cookie notification state
    private bool ShowCookieNotification = false;

    // Confirmation dialog state
    private bool ShowConfirmDialog = false;
    private string ConfirmDialogTitle = "";
    private string ConfirmDialogMessage = "";
    private Action? ConfirmDialogAction = null;
    private bool ShowSqlModal = false;
    private string GeneratedSqlOutput = string.Empty;
    private IJSObjectReference? jsModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/diagramer.js");
                await jsModule.InvokeVoidAsync("initDragDrop", DotNetObjectReference.Create(this));

                // Check if cookie notification has been shown
                bool notificationShown = await jsModule.InvokeAsync<bool>("checkCookieNotificationShown");
                if (!notificationShown)
                {
                    ShowCookieNotification = true;
                    StateHasChanged();
                }

                // Load diagram from cookie backup if available
                string? backupJson = await jsModule.InvokeAsync<string>("getCookie", "diagramer_backup");
                if (!string.IsNullOrEmpty(backupJson))
                {
                    try
                    {
                        await LoadDiagramFromJson(backupJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading backup from cookie: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing drag-drop: {ex.Message}");
            }
        }
    }

    protected override void OnInitialized()
    {
        UpdateJsonOutput();
    }

    private void AddTableViaButton()
    {
        var newTable = new TableModel
        {
            Name = $"Table_{Tables.Count + 1}",
            X = 400,
            Y = 50
        };
        Tables.Add(newTable);
        UpdateJsonOutput();
    }

    private void AddTextLabelViaButton()
    {
        var label = new TextLabelModel
        {
            Text = $"Label {TextLabels.Count + 1}",
            X = 420,
            Y = 120 + (TextLabels.Count * 36)
        };

        TextLabels.Add(label);
        EditingTextLabelId = label.Id;
        UpdateJsonOutput();
    }

    private void OnDragStartTable(DragEventArgs args)
    {
        // Handled by JavaScript now
    }

    private void OnDragStartRelation(DragEventArgs args)
    {
        // Handled by JavaScript now
    }

    private void OnDragStartCellRelation(DragEventArgs args, string tableId, int rowIndex)
    {
        IsPanningCanvas = false;
        DrawingRelation = new DrawingRelationModel
        {
            FromTableId = tableId,
            FromRowIndex = rowIndex,
            StartX = args.ClientX,
            StartY = args.ClientY,
            CurrentX = args.ClientX,
            CurrentY = args.ClientY
        };
    }

    private void OnDragEndCellRelation(DragEventArgs args)
    {
        IsPanningCanvas = false;
        DrawingRelation = null;
    }

    private void OnRelationHandleDragOver(DragEventArgs args)
    {
        // Allow drop on relation handles
    }

    private async Task OnDropRelation(DragEventArgs args, string tableId, int rowIndex)
    {
        IsPanningCanvas = false;
        if (DrawingRelation == null) return;

        // Prevent dropping on the same row of the same table
        if (DrawingRelation.FromTableId == tableId && DrawingRelation.FromRowIndex == rowIndex)
        {
            DrawingRelation = null;
            return;
        }

        var fromTable = Tables.FirstOrDefault(t => t.Id == DrawingRelation.FromTableId);
        var toTable = Tables.FirstOrDefault(t => t.Id == tableId);

        if (fromTable == null || toTable == null)
        {
            DrawingRelation = null;
            return;
        }

        var fromRow = fromTable.Rows.ElementAtOrDefault(DrawingRelation.FromRowIndex);
        var toRow = toTable.Rows.ElementAtOrDefault(rowIndex);

        if (fromRow == null || toRow == null)
        {
            DrawingRelation = null;
            return;
        }

        // Validate that datatypes match
        if (!string.Equals(fromRow.DataType, toRow.DataType, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(fromRow.DataType)
            || string.IsNullOrEmpty(toRow.DataType))
        {
            DrawingRelation = null;
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("showErrorNotification",
                    "The datatypes of the rows are different, please make sure the datatype of both rows matches.");
            }
            return;
        }

        // Create relation with format [Origin_Table]_[Origin_Cell]_[Destination_Table]_[Destination_Cell]_FK
        var relationName = $"{fromTable.Name}_{fromRow.Name}_{toTable.Name}_{toRow.Name}_FK";

        var relation = new RelationModel
        {
            FromTableId = DrawingRelation.FromTableId,
            FromRowIndex = DrawingRelation.FromRowIndex,
            ToTableId = tableId,
            ToRowIndex = rowIndex,
            Name = relationName
        };

        Relations.Add(relation);
        DrawingRelation = null;
        UpdateJsonOutput();
    }

    private void OnCanvasDragOver(DragEventArgs args)
    {
        args.DataTransfer!.DropEffect = "copy";
    }

    private void OnCanvasMouseMove(MouseEventArgs args)
    {
        if (IsPanningCanvas)
        {
            CanvasOffsetX = PanStartOffsetX + (args.ClientX - PanStartClientX);
            CanvasOffsetY = PanStartOffsetY + (args.ClientY - PanStartClientY);
            UpdateJsonOutput();
            StateHasChanged();
            return;
        }

        var (diagramX, diagramY) = ToDiagramCoordinates(args.ClientX, args.ClientY);

        if (DraggingTable != null)
        {
            DraggingTable.X = diagramX - DragOffsetX;
            DraggingTable.Y = diagramY - DragOffsetY;
            StateHasChanged();
        }

        if (DraggingTextLabel != null)
        {
            DraggingTextLabel.X = diagramX - DragOffsetX;
            DraggingTextLabel.Y = diagramY - DragOffsetY;
            StateHasChanged();
        }

        if (ResizingTable != null)
        {
            double deltaX = diagramX - ResizeStartX;
            double deltaY = diagramY - ResizeStartY;

            ResizingTable.Width = Math.Max(200, ResizeStartWidth + deltaX);
            ResizingTable.Height = Math.Max(300, ResizeStartHeight + deltaY);
            StateHasChanged();
        }

        if (DraggingWaypointRelationId != null)
        {
            var relation = Relations.FirstOrDefault(r => r.Id == DraggingWaypointRelationId);
            if (relation != null)
            {
                relation.ManualRouteX = WaypointStartRouteX + (diagramX - WaypointDragStartX);
            }
            StateHasChanged();
        }

        if (DrawingRelation != null)
        {
            DrawingRelation.CurrentX = diagramX;
            DrawingRelation.CurrentY = diagramY;
            StateHasChanged();
        }
    }

    private void OnCanvasMouseUp()
    {
        var diagramChanged = DraggingTable != null
            || DraggingTextLabel != null
            || ResizingTable != null
            || DraggingWaypointRelationId != null;

        DraggingTable = null;
        DraggingTextLabel = null;
        ResizingTable = null;
        DraggingWaypointRelationId = null;
        IsPanningCanvas = false;

        if (diagramChanged)
        {
            UpdateJsonOutput();
        }

        if (_relationClickedThisFrame)
        {
            _relationClickedThisFrame = false;
        }
        else
        {
            SelectedRelationId = null;
        }
    }

    private void OnDragEnd(DragEventArgs args)
    {
        DraggedItemType = "";
    }

    private void OnTableMouseDown(TableModel table, MouseEventArgs args)
    {
        var (diagramX, diagramY) = ToDiagramCoordinates(args.ClientX, args.ClientY);
        DraggingTable = table;
        DragOffsetX = diagramX - table.X;
        DragOffsetY = diagramY - table.Y;
    }

    private void OnTextLabelMouseDown(TextLabelModel label, MouseEventArgs args)
    {
        if (EditingTextLabelId == label.Id)
            return;

        var (diagramX, diagramY) = ToDiagramCoordinates(args.ClientX, args.ClientY);
        DraggingTextLabel = label;
        DragOffsetX = diagramX - label.X;
        DragOffsetY = diagramY - label.Y;
    }

    private void StartTextLabelEditing(string labelId)
    {
        EditingTextLabelId = labelId;
    }

    private void FinishTextLabelEditing()
    {
        EditingTextLabelId = null;
        UpdateJsonOutput();
    }

    private void RemoveTextLabel(string labelId)
    {
        TextLabels.RemoveAll(l => l.Id == labelId);
        if (EditingTextLabelId == labelId)
        {
            EditingTextLabelId = null;
        }
        UpdateJsonOutput();
    }

    private void HandleTextLabelKeyUp(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            FinishTextLabelEditing();
        }
        else if (args.Key == "Escape")
        {
            EditingTextLabelId = null;
        }
    }

    private void OnTableMouseMove(MouseEventArgs args)
    {
        if (DraggingTable != null)
        {
            DraggingTable.X = args.ClientX - DragOffsetX;
            DraggingTable.Y = args.ClientY - DragOffsetY;
            StateHasChanged();
        }
    }

    private void OnTableMouseUp()
    {
        DraggingTable = null;
    }

    private void OnResizeMouseDown(TableModel table, MouseEventArgs args)
    {
        var (diagramX, diagramY) = ToDiagramCoordinates(args.ClientX, args.ClientY);
        ResizingTable = table;
        ResizeStartX = diagramX;
        ResizeStartY = diagramY;
        ResizeStartWidth = table.Width;
        ResizeStartHeight = table.Height;
    }

    private void AddRow(string tableId)
    {
        var table = Tables.FirstOrDefault(t => t.Id == tableId);
        if (table != null)
        {
            table.Rows.Add(new ColumnModel());
            UpdateJsonOutput();
        }
    }

    private void RemoveRow(string tableId, int rowIndex)
    {
        var table = Tables.FirstOrDefault(t => t.Id == tableId);
        if (table != null && rowIndex >= 0 && rowIndex < table.Rows.Count)
        {
            var rowName = string.IsNullOrWhiteSpace(table.Rows[rowIndex].Name) ? $"Row {rowIndex + 1}" : table.Rows[rowIndex].Name;
            var affectedRelations = Relations.Where(r =>
                (r.FromTableId == tableId && r.FromRowIndex == rowIndex) ||
                (r.ToTableId == tableId && r.ToRowIndex == rowIndex)).ToList();

            var message = $"Are you sure you want to delete the column \"{rowName}\" from table \"{table.Name}\"?";
            if (affectedRelations.Count > 0)
            {
                var relationNames = string.Join(", ", affectedRelations.Select(r => r.Name));
                message += $"\n\nThis will also delete {affectedRelations.Count} relation(s): {relationNames}";
            }

            ShowConfirmDialog = true;
            ConfirmDialogTitle = "Delete Column";
            ConfirmDialogMessage = message;
            ConfirmDialogAction = () =>
            {
                // Remove relations that reference this row
                Relations.RemoveAll(r =>
                    (r.FromTableId == tableId && r.FromRowIndex == rowIndex) ||
                    (r.ToTableId == tableId && r.ToRowIndex == rowIndex));

                // Adjust row indices for relations referencing rows after the deleted one
                foreach (var relation in Relations)
                {
                    if (relation.FromTableId == tableId && relation.FromRowIndex > rowIndex)
                        relation.FromRowIndex--;
                    if (relation.ToTableId == tableId && relation.ToRowIndex > rowIndex)
                        relation.ToRowIndex--;
                }

                table.Rows.RemoveAt(rowIndex);
                if (SelectedTableId == tableId && SelectedRowIndex == rowIndex)
                {
                    SelectedRowIndex = -1;
                    SelectedTableId = "";
                }
                if (SelectedRelationId != null && !Relations.Any(r => r.Id == SelectedRelationId))
                    SelectedRelationId = null;

                UpdateRelationNames();
                UpdateJsonOutput();
            };
        }
    }

    private void SelectRow(string tableId, int rowIndex)
    {
        if (SelectedTableId == tableId && SelectedRowIndex == rowIndex)
        {
            SelectedRowIndex = -1;
            SelectedTableId = "";
        }
        else
        {
            SelectedTableId = tableId;
            SelectedRowIndex = rowIndex;
        }
    }

    private void HandleDataTypeChange(ChangeEventArgs e, ColumnModel row)
    {
        if (e.Value != null)
        {
            row.DataType = e.Value.ToString() ?? "";
            UpdateJsonOutput();
        }
    }

    private void HandlePrimaryKeyChange(ChangeEventArgs e, ColumnModel row)
    {
        if (e.Value != null)
        {
            row.PrimaryKey = e.Value.ToString() ?? "";
            UpdateJsonOutput();
        }
    }

    private void HandleNullChange(ChangeEventArgs e, ColumnModel row)
    {
        if (e.Value != null)
        {
            row.Null = e.Value.ToString() ?? "";
            UpdateJsonOutput();
        }
    }

    private void MoveRowUp(string tableId)
    {
        var table = Tables.FirstOrDefault(t => t.Id == tableId);
        if (table != null && SelectedRowIndex > 0 && SelectedRowIndex < table.Rows.Count)
        {
            var temp = table.Rows[SelectedRowIndex];
            table.Rows[SelectedRowIndex] = table.Rows[SelectedRowIndex - 1];
            table.Rows[SelectedRowIndex - 1] = temp;
            SelectedRowIndex--;
            UpdateJsonOutput();
        }
    }

    private void MoveRowDown(string tableId)
    {
        var table = Tables.FirstOrDefault(t => t.Id == tableId);
        if (table != null && SelectedRowIndex >= 0 && SelectedRowIndex < table.Rows.Count - 1)
        {
            var temp = table.Rows[SelectedRowIndex];
            table.Rows[SelectedRowIndex] = table.Rows[SelectedRowIndex + 1];
            table.Rows[SelectedRowIndex + 1] = temp;
            SelectedRowIndex++;
            UpdateJsonOutput();
        }
    }

    private void RemoveTable(string tableId)
    {
        Tables.RemoveAll(t => t.Id == tableId);
        Relations.RemoveAll(r => r.FromTableId == tableId || r.ToTableId == tableId);
        UpdateJsonOutput();
    }

    private void HandleTitleKeyUp(KeyboardEventArgs args, TableModel table)
    {
        if (args.Key == "Enter")
        {
            EditingTableId = null;
            UpdateRelationNames();
            UpdateJsonOutput();
        }
    }

    private void UpdateJsonOutput()
    {
        var schema = new
        {
            viewport = new
            {
                zoom = CanvasZoom,
                offset = new { x = CanvasOffsetX, y = CanvasOffsetY }
            },
            tables = Tables.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                position = new { x = t.X, y = t.Y },
                size = new { width = t.Width, height = t.Height },
                columns = t.Rows.Select(r => new
                {
                    name = r.Name,
                    dataType = r.DataType,
                    length = r.Length,
                    defaultValue = r.DefaultValue,
                    nullable = r.Null,
                    primaryKey = r.PrimaryKey
                }).ToList()
            }).ToList(),
            relations = Relations.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                hasCustomName = r.HasCustomName ? true : (bool?)null,
                fromTable = r.FromTableId,
                fromColumn = r.FromRowIndex,
                toTable = r.ToTableId,
                toColumn = r.ToRowIndex,
                manualRouteX = r.ManualRouteX
            }).ToList(),
            labels = TextLabels.Select(l => new
            {
                id = l.Id,
                text = l.Text,
                x = l.X,
                y = l.Y
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        JsonOutput = JsonSerializer.Serialize(schema, options);

        // Save to cookie backup (2 hours)
        if (jsModule != null)
        {
            _ = jsModule.InvokeVoidAsync("setCookie", "diagramer_backup", JsonOutput, 2);
        }
    }

    private void UpdateRelationNames()
    {
        foreach (var relation in Relations)
        {
            if (relation.HasCustomName) continue;

            var fromTable = Tables.FirstOrDefault(t => t.Id == relation.FromTableId);
            var toTable = Tables.FirstOrDefault(t => t.Id == relation.ToTableId);
            if (fromTable == null || toTable == null) continue;

            var fromRow = fromTable.Rows.ElementAtOrDefault(relation.FromRowIndex);
            var toRow = toTable.Rows.ElementAtOrDefault(relation.ToRowIndex);
            if (fromRow == null || toRow == null) continue;

            relation.Name = $"{fromTable.Name}_{fromRow.Name}_{toTable.Name}_{toRow.Name}_FK";
        }
    }

    private void SelectRelation(string relationId)
    {
        SelectedRelationId = SelectedRelationId == relationId ? null : relationId;
        _relationClickedThisFrame = true;
    }

    private void StartEditingRelationName(string relationId)
    {
        EditingRelationId = relationId;
        _relationClickedThisFrame = true;
    }

    private void FinishEditingRelationName(string relationId)
    {
        var relation = Relations.FirstOrDefault(r => r.Id == relationId);
        if (relation != null)
        {
            relation.HasCustomName = true;
        }
        EditingRelationId = null;
        UpdateJsonOutput();
    }

    private void HandleRelationNameKeyUp(KeyboardEventArgs args, string relationId)
    {
        if (args.Key == "Enter")
        {
            FinishEditingRelationName(relationId);
        }
        else if (args.Key == "Escape")
        {
            EditingRelationId = null;
        }
    }

    private void RemoveRelation(string relationId)
    {
        var relation = Relations.FirstOrDefault(r => r.Id == relationId);
        if (relation != null)
        {
            ShowConfirmDialog = true;
            ConfirmDialogTitle = "Delete Relation";
            ConfirmDialogMessage = $"Are you sure you want to delete the relation \"{relation.Name}\"?";
            ConfirmDialogAction = () =>
            {
                Relations.RemoveAll(r => r.Id == relationId);
                SelectedRelationId = null;
                UpdateJsonOutput();
            };
            _relationClickedThisFrame = true;
        }
    }

    private void ConfirmDelete()
    {
        ConfirmDialogAction?.Invoke();
        ShowConfirmDialog = false;
        ConfirmDialogAction = null;
    }

    private void CancelDelete()
    {
        ShowConfirmDialog = false;
        ConfirmDialogAction = null;
    }

    private void GenerateSqlScript()
    {
        var sqlBuilder = new System.Text.StringBuilder();

        for (int tableIndex = 0; tableIndex < Tables.Count; tableIndex++)
        {
            var table = Tables[tableIndex];
            var primaryKeyColumns = table.Rows.Where(r => string.Equals(r.PrimaryKey, "Yes", StringComparison.OrdinalIgnoreCase)).ToList();

            sqlBuilder.AppendLine($"CREATE TABLE [dbo].[{EscapeSqlIdentifier(table.Name)}] (");

            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var column = table.Rows[rowIndex];
                var isLastColumnDefinition = rowIndex == table.Rows.Count - 1 && primaryKeyColumns.Count == 0;
                var comma = isLastColumnDefinition ? string.Empty : ",";

                sqlBuilder.AppendLine($"    [{EscapeSqlIdentifier(column.Name)}] {GetSqlServerColumnDefinition(column)}{comma}");
            }

            if (primaryKeyColumns.Count > 0)
            {
                var primaryKeyName = $"PK_{table.Name}";
                var pkColumns = string.Join(", ", primaryKeyColumns.Select(c => $"[{EscapeSqlIdentifier(c.Name)}] ASC"));
                sqlBuilder.AppendLine($"    CONSTRAINT [{EscapeSqlIdentifier(primaryKeyName)}] PRIMARY KEY CLUSTERED");
                sqlBuilder.AppendLine("    (");
                sqlBuilder.AppendLine($"        {pkColumns}");
                sqlBuilder.AppendLine("    )");
            }

            sqlBuilder.AppendLine(");");

            if (tableIndex < Tables.Count - 1 || Relations.Count > 0)
            {
                sqlBuilder.AppendLine();
            }
        }

        for (int relationIndex = 0; relationIndex < Relations.Count; relationIndex++)
        {
            var relation = Relations[relationIndex];
            var fromTable = Tables.FirstOrDefault(t => t.Id == relation.FromTableId);
            var toTable = Tables.FirstOrDefault(t => t.Id == relation.ToTableId);
            var fromColumn = fromTable?.Rows.ElementAtOrDefault(relation.FromRowIndex);
            var toColumn = toTable?.Rows.ElementAtOrDefault(relation.ToRowIndex);

            if (fromTable == null || toTable == null || fromColumn == null || toColumn == null)
            {
                continue;
            }

            sqlBuilder.AppendLine($"ALTER TABLE [dbo].[{EscapeSqlIdentifier(toTable.Name)}]  WITH CHECK ADD  CONSTRAINT [{EscapeSqlIdentifier(relation.Name)}] FOREIGN KEY([{EscapeSqlIdentifier(toColumn.Name)}])");
            sqlBuilder.AppendLine($"REFERENCES [dbo].[{EscapeSqlIdentifier(fromTable.Name)}] ([{EscapeSqlIdentifier(fromColumn.Name)}]);");
            sqlBuilder.AppendLine($"ALTER TABLE [dbo].[{EscapeSqlIdentifier(toTable.Name)}] CHECK CONSTRAINT [{EscapeSqlIdentifier(relation.Name)}];");

            if (relationIndex < Relations.Count - 1)
            {
                sqlBuilder.AppendLine();
            }
        }

        GeneratedSqlOutput = sqlBuilder.ToString().TrimEnd();
        ShowSqlModal = true;
    }

    private static string EscapeSqlIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return "Unnamed";

        // Validate identifier length (SQL Server limit is 128 characters)
        if (identifier.Length > 128)
            identifier = identifier.Substring(0, 128);

        // Escape special characters for SQL Server identifiers
        identifier = identifier.Replace("]", "]]", StringComparison.Ordinal);

        // Remove or replace potentially dangerous characters
        // Allow only alphanumeric, underscore, and numbers (but not starting with number)
        var sanitized = System.Text.RegularExpressions.Regex.Replace(identifier, @"[^\w\s]", "_");

        if (string.IsNullOrWhiteSpace(sanitized))
            return "Unnamed";

        return sanitized;
    }

    private static string GetSqlServerColumnDefinition(ColumnModel column)
    {
        var dataType = string.IsNullOrWhiteSpace(column.DataType) ? "nvarchar" : column.DataType.Trim();
        var normalizedType = dataType.ToLowerInvariant();
        var typesWithLength = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "char", "varchar", "nchar", "nvarchar", "binary", "varbinary"
        };

        var typesWithPrecision = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decimal", "numeric"
        };

        string sqlType;
        if (typesWithLength.Contains(normalizedType) && !normalizedType.Contains("max", StringComparison.OrdinalIgnoreCase))
        {
            var length = column.Length.GetValueOrDefault(normalizedType.StartsWith("n", StringComparison.OrdinalIgnoreCase) ? 50 : 100);
            sqlType = $"{dataType}({length})";
        }
        else if (typesWithPrecision.Contains(normalizedType))
        {
            sqlType = $"{dataType}(18, 2)";
        }
        else
        {
            sqlType = dataType;
        }
        var identity = string.Equals(column.PrimaryKey, "Yes", StringComparison.OrdinalIgnoreCase) && column.DataType.Contains("int") ? "IDENTITY(1,1)" 
            : string.Equals(column.PrimaryKey, "Yes", StringComparison.OrdinalIgnoreCase) ? "IDENTITY" : "";
        var nullability = string.Equals(column.Null, "Yes", StringComparison.OrdinalIgnoreCase) ? "NULL" : "NOT NULL";
        return $"{sqlType} {identity} {nullability}";
    }

    private void CloseSqlModal()
    {
        ShowSqlModal = false;
    }

    private async Task CloseCookieNotification()
    {
        ShowCookieNotification = false;
        if (jsModule != null)
        {
            await jsModule.InvokeVoidAsync("setCookieNotificationShown");
        }
    }

    private async Task CopySqlToClipboard()
    {
        if (jsModule == null)
            return;

        var success = await jsModule.InvokeAsync<bool>("copyToClipboard", GeneratedSqlOutput);
        if (success)
        {
            await jsModule.InvokeVoidAsync("showNotification", "SQL copied to clipboard!");
        }
        else
        {
            await jsModule.InvokeVoidAsync("showErrorNotification", "Failed to copy SQL to clipboard.");
        }
    }

    // Layout constants for connection point calculation
    private const double TableHeaderHeight = 50;
    private const double FixedRowHeight = 40;
    private const double DataRowHeight = 40;
    private const double RouteMargin = 30;

    private (double X, double Y) GetConnectionPoint(TableModel table, int rowIndex)
    {
        double x = table.X;
        double y = table.Y + TableHeaderHeight + FixedRowHeight + (rowIndex * DataRowHeight) + (DataRowHeight / 2);
        return (x, y);
    }

    private List<(double X, double Y)> ComputeRelationPath(RelationModel relation)
    {
        var fromTable = Tables.FirstOrDefault(t => t.Id == relation.FromTableId);
        var toTable = Tables.FirstOrDefault(t => t.Id == relation.ToTableId);
        if (fromTable == null || toTable == null) return new();

        var (fromX, fromY) = GetConnectionPoint(fromTable, relation.FromRowIndex);
        var (toX, toY) = GetConnectionPoint(toTable, relation.ToRowIndex);

        double routeX;
        if (relation.ManualRouteX.HasValue)
        {
            routeX = relation.ManualRouteX.Value;
        }
        else
        {
            // Auto-compute: go left of both tables with margin
            routeX = Math.Min(fromX, toX) - RouteMargin;

            // Push further left if any table blocks the vertical segment
            double minY = Math.Min(fromY, toY);
            double maxY = Math.Max(fromY, toY);

            foreach (var table in Tables)
            {
                // Skip the source and destination tables themselves
                if (table.Id == relation.FromTableId || table.Id == relation.ToTableId)
                    continue;

                double tLeft = table.X;
                double tRight = table.X + table.Width;
                double tTop = table.Y;
                double tBottom = table.Y + table.Height;

                if (routeX >= tLeft && routeX <= tRight &&
                    tTop < maxY && tBottom > minY)
                {
                    routeX = Math.Min(routeX, tLeft - RouteMargin);
                }
            }
        }

        return new List<(double X, double Y)>
        {
            (fromX, fromY),
            (routeX, fromY),
            (routeX, toY),
            (toX, toY)
        };
    }

    private void OnWaypointMouseDown(MouseEventArgs args, string relationId)
    {
        var relation = Relations.FirstOrDefault(r => r.Id == relationId);
        if (relation != null)
        {
            var path = ComputeRelationPath(relation);
            if (path.Count >= 3)
            {
                var (diagramX, _) = ToDiagramCoordinates(args.ClientX, args.ClientY);
                WaypointDragStartX = diagramX;
                WaypointStartRouteX = relation.ManualRouteX ?? path[1].X;
            }
            DraggingWaypointRelationId = relationId;
            _relationClickedThisFrame = true;
        }
    }

    private void OnRelationLineMouseDown(MouseEventArgs args, string relationId)
    {
        SelectRelation(relationId);
    }

    private void OnRelationLineSegmentMouseDown(MouseEventArgs args, string relationId, int segmentIndex)
    {
        var relation = Relations.FirstOrDefault(r => r.Id == relationId);
        if (relation != null)
        {
            var path = ComputeRelationPath(relation);
            if (path.Count >= 4)
            {
                var (diagramX, _) = ToDiagramCoordinates(args.ClientX, args.ClientY);
                WaypointDragStartX = diagramX;
                WaypointStartRouteX = relation.ManualRouteX ?? path[1].X;
                            DraggingWaypointRelationId = relationId;
                            _relationClickedThisFrame = true;
                        }
                    }
                }

                private void OnCanvasBackgroundMouseDown(MouseEventArgs args)
    {
        if (DraggingTable != null || ResizingTable != null || DraggingWaypointRelationId != null || ShowConfirmDialog)
            return;

        IsPanningCanvas = true;
        PanStartClientX = args.ClientX;
        PanStartClientY = args.ClientY;
        PanStartOffsetX = CanvasOffsetX;
        PanStartOffsetY = CanvasOffsetY;
    }

    private (double X, double Y) ToDiagramCoordinates(double clientX, double clientY)
    {
        return ((clientX - CanvasOffsetX) / CanvasZoom, (clientY - CanvasOffsetY) / CanvasZoom);
    }

    private void ZoomIn()
    {
        CanvasZoom = Math.Min(2.5, CanvasZoom + 0.1);
        UpdateJsonOutput();
    }

    private void ZoomOut()
    {
        CanvasZoom = Math.Max(0.4, CanvasZoom - 0.1);
        UpdateJsonOutput();
    }

    private void ResetViewport()
    {
        CanvasZoom = 1;
        CanvasOffsetX = 0;
        CanvasOffsetY = 0;
        UpdateJsonOutput();
    }

    [JSInvokable]
    public async Task OnDragDropTable(double x, double y)
    {
        var (diagramX, diagramY) = ToDiagramCoordinates(x, y);
        var newTable = new TableModel
        {
            Name = $"Table_{Tables.Count + 1}",
            X = diagramX - 100,
            Y = diagramY - 50
        };
        Tables.Add(newTable);
        UpdateJsonOutput();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ExportDiagram()
    {
        if (jsModule == null)
            return;

        try
        {
            await jsModule.InvokeVoidAsync("exportDiagramWithSavePicker", "diagram-export-surface");
            await jsModule.InvokeVoidAsync("showNotification", "Diagram exported!");
        }
        catch (Exception ex)
        {
            // Silently ignore user cancellation
            if (ex.Message.Contains("aborted", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Console.WriteLine($"Error exporting diagram: {ex.Message}");
            await jsModule.InvokeVoidAsync("showErrorNotification", $"Error exporting diagram: {ex.Message}");
        }
    }

    private async Task CopyJsonToClipboard()
    {
        try
        {
            if (jsModule != null)
            {
                bool success = await jsModule.InvokeAsync<bool>("copyToClipboard", JsonOutput);
                if (success)
                {
                    await jsModule.InvokeVoidAsync("showNotification", "JSON copied to clipboard!");
                }
                else
                {
                    await jsModule.InvokeVoidAsync("showNotification", "Failed to copy to clipboard", 3000);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying to clipboard: {ex.Message}");
        }
    }

    private async Task DownloadJsonFile()
    {
        try
        {
            var fileName = "diagram.json";

            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("downloadFile", JsonOutput, fileName, "application/json");
                await jsModule.InvokeVoidAsync("showNotification", "JSON file downloaded!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("showNotification", $"Error downloading file: {ex.Message}", 3000);
            }
        }
    }

    private async Task TriggerFileUpload()
    {
        try
        {
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("triggerFileInput");
            }
            else
            {
                Console.WriteLine("JS module not loaded yet");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error triggering file upload: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task HandleFileUpload(ChangeEventArgs e)
    {
        try
        {
            Console.WriteLine("HandleFileUpload called");

            if (jsModule == null)
            {
                Console.WriteLine("JS module is null, cannot read file");
                return;
            }

            var fileContent = await jsModule.InvokeAsync<string>("readUploadedFile");
            Console.WriteLine($"File content length: {fileContent?.Length ?? 0}");

            // Security: Validate file size (5MB limit)
            const int MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
            if (!string.IsNullOrEmpty(fileContent) && fileContent.Length > MAX_FILE_SIZE)
            {
                if (jsModule != null)
                {
                    await jsModule.InvokeVoidAsync("showErrorNotification", "File is too large. Maximum size is 5MB.", 4000);
                }
                return;
            }

            if (!string.IsNullOrEmpty(fileContent))
            {
                Console.WriteLine("Loading diagram from JSON");
                // Validate and load JSON
                await LoadDiagramFromJson(fileContent);
            }
            else
            {
                Console.WriteLine("File content is empty");
                if (jsModule != null)
                {
                    await jsModule.InvokeVoidAsync("showNotification", "No file was selected", 2000);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling file upload: {ex.Message}");
            // Don't expose stack traces to users - security measure
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("showErrorNotification", "Error loading file. Please ensure the file is valid JSON.", 3000);
            }
        }
    }

    private async Task LoadDiagramFromJson(string jsonContent)
    {
        try
        {
            Console.WriteLine("Starting LoadDiagramFromJson");
            Console.WriteLine($"JSON content preview: {(jsonContent.Length > 100 ? jsonContent.Substring(0, 100) + "..." : jsonContent)}");

            // Parse and validate JSON structure
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                var root = doc.RootElement;
                Console.WriteLine($"JSON root element kind: {root.ValueKind}");

                // Validate required structure
                if (!root.TryGetProperty("tables", out var tablesElement) || tablesElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("Invalid diagram format: missing or invalid 'tables' array");
                }

                Console.WriteLine($"Found {tablesElement.GetArrayLength()} tables");

                // Clear current diagram
                Tables.Clear();
                Relations.Clear();
                TextLabels.Clear();

                if (root.TryGetProperty("viewport", out var viewportElement) && viewportElement.ValueKind == JsonValueKind.Object)
                {
                    if (viewportElement.TryGetProperty("zoom", out var zoomElement) && zoomElement.ValueKind == JsonValueKind.Number)
                        CanvasZoom = Math.Clamp(zoomElement.GetDouble(), 0.4, 2.5);

                    if (viewportElement.TryGetProperty("offset", out var offsetElement) && offsetElement.ValueKind == JsonValueKind.Object)
                    {
                        if (offsetElement.TryGetProperty("x", out var offsetXElement) && offsetXElement.ValueKind == JsonValueKind.Number)
                            CanvasOffsetX = offsetXElement.GetDouble();

                        if (offsetElement.TryGetProperty("y", out var offsetYElement) && offsetYElement.ValueKind == JsonValueKind.Number)
                            CanvasOffsetY = offsetYElement.GetDouble();
                    }
                }
                else
                {
                    CanvasZoom = 1;
                    CanvasOffsetX = 0;
                    CanvasOffsetY = 0;
                }

                // Load tables
                foreach (var tableElement in tablesElement.EnumerateArray())
                {
                    var table = new TableModel();

                    if (tableElement.TryGetProperty("id", out var idElement))
                        table.Id = idElement.GetString() ?? Guid.NewGuid().ToString();

                    if (tableElement.TryGetProperty("name", out var nameElement))
                        table.Name = nameElement.GetString() ?? "New Table";

                    if (tableElement.TryGetProperty("position", out var posElement))
                    {
                        if (posElement.TryGetProperty("x", out var xElement))
                            table.X = xElement.GetDouble();
                        if (posElement.TryGetProperty("y", out var yElement))
                            table.Y = yElement.GetDouble();
                    }

                    if (tableElement.TryGetProperty("size", out var sizeElement))
                    {
                        if (sizeElement.TryGetProperty("width", out var widthElement))
                            table.Width = widthElement.GetDouble();
                        if (sizeElement.TryGetProperty("height", out var heightElement))
                            table.Height = heightElement.GetDouble();
                    }

                    // Load columns
                    if (tableElement.TryGetProperty("columns", out var columnsElement) && columnsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var columnElement in columnsElement.EnumerateArray())
                        {
                            var column = new ColumnModel();

                            if (columnElement.TryGetProperty("name", out var colNameElement))
                                column.Name = colNameElement.GetString() ?? "";

                            if (columnElement.TryGetProperty("dataType", out var dataTypeElement))
                                column.DataType = dataTypeElement.GetString() ?? "";

                            if (columnElement.TryGetProperty("length", out var lengthElement) && lengthElement.ValueKind == JsonValueKind.Number)
                                column.Length = lengthElement.GetInt32();

                            if (columnElement.TryGetProperty("defaultValue", out var defaultElement))
                                column.DefaultValue = defaultElement.GetString() ?? "";

                            if (columnElement.TryGetProperty("nullable", out var nullElement))
                                column.Null = nullElement.GetString() ?? "";

                            if (columnElement.TryGetProperty("primaryKey", out var pkElement))
                                column.PrimaryKey = pkElement.GetString() ?? "";

                            table.Rows.Add(column);
                        }
                    }

                    Console.WriteLine($"Added table: {table.Name} with {table.Rows.Count} columns");
                    Tables.Add(table);
                }

                if (root.TryGetProperty("labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var labelElement in labelsElement.EnumerateArray())
                    {
                        var label = new TextLabelModel();

                        if (labelElement.TryGetProperty("id", out var labelIdElement))
                            label.Id = labelIdElement.GetString() ?? Guid.NewGuid().ToString();

                        if (labelElement.TryGetProperty("text", out var labelTextElement))
                            label.Text = labelTextElement.GetString() ?? "New label";

                        if (labelElement.TryGetProperty("x", out var labelXElement) && labelXElement.ValueKind == JsonValueKind.Number)
                            label.X = labelXElement.GetDouble();

                        if (labelElement.TryGetProperty("y", out var labelYElement) && labelYElement.ValueKind == JsonValueKind.Number)
                            label.Y = labelYElement.GetDouble();

                        TextLabels.Add(label);
                    }
                }

                // Load relations if they exist
                if (root.TryGetProperty("relations", out var relationsElement) && relationsElement.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine($"Found {relationsElement.GetArrayLength()} relations");

                    foreach (var relationElement in relationsElement.EnumerateArray())
                    {
                        var relation = new RelationModel();

                        if (relationElement.TryGetProperty("id", out var relIdElement))
                            relation.Id = relIdElement.GetString() ?? Guid.NewGuid().ToString();

                        if (relationElement.TryGetProperty("name", out var relNameElement))
                            relation.Name = relNameElement.GetString() ?? "New Relation";

                        if (relationElement.TryGetProperty("fromTable", out var fromTableElement))
                            relation.FromTableId = fromTableElement.GetString() ?? "";

                        if (relationElement.TryGetProperty("fromColumn", out var fromColElement))
                            relation.FromRowIndex = fromColElement.GetInt32();

                        if (relationElement.TryGetProperty("toTable", out var toTableElement))
                            relation.ToTableId = toTableElement.GetString() ?? "";

                        if (relationElement.TryGetProperty("toColumn", out var toColElement))
                            relation.ToRowIndex = toColElement.GetInt32();

                        if (relationElement.TryGetProperty("hasCustomName", out var customNameElement) && customNameElement.ValueKind == JsonValueKind.True)
                            relation.HasCustomName = true;

                        if (relationElement.TryGetProperty("manualRouteX", out var routeXElement) && routeXElement.ValueKind == JsonValueKind.Number)
                            relation.ManualRouteX = routeXElement.GetDouble();

                        // Only add relation if both tables exist
                        if (Tables.Any(t => t.Id == relation.FromTableId) && 
                            Tables.Any(t => t.Id == relation.ToTableId))
                        {
                            Console.WriteLine($"Added relation: {relation.Name}");
                            Relations.Add(relation);
                        }
                        else
                        {
                            Console.WriteLine($"Skipping relation {relation.Name}: missing table references");
                        }
                    }
                }

                Console.WriteLine("Updating JSON output");
                UpdateJsonOutput();

                Console.WriteLine("Triggering state change");
                await InvokeAsync(StateHasChanged);

                if (jsModule != null)
                {
                    await jsModule.InvokeVoidAsync("setCookie", "diagramer_backup", JsonOutput, 2);
                    await jsModule.InvokeVoidAsync("showNotification", "Diagram loaded successfully!");
                }

                Console.WriteLine("LoadDiagramFromJson completed successfully");
            }
        }
        catch (JsonException jex)
        {
            Console.WriteLine($"JSON parsing error: {jex.Message}");
            Console.WriteLine($"Stack trace: {jex.StackTrace}");
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("showNotification", $"Invalid JSON format: {jex.Message}", 3000);
            }
        }
        catch (InvalidOperationException iex)
        {
            Console.WriteLine($"Validation error: {iex.Message}");
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("showNotification", iex.Message, 3000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading diagram: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("showNotification", $"Error loading diagram: {ex.Message}", 3000);
            }
        }
    }
}