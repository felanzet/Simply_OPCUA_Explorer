using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Quickstarts.DataAccessClient
{
    /// <summary>
    /// Pop-up window that subscribes to a fixed list of tags read from
    /// OPCUA_Config_Monitor_Table.txt, shows only the last part of each tag name,
    /// lets the user change the scan (sampling) time and, for writable variables,
    /// edit the value directly in the grid.
    /// </summary>
    public partial class MonitoredTableDlg : Form
    {
        #region Constructors

        public MonitoredTableDlg()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Fields

        private Session m_session;

        /*
         * The Parker OPC UA server reports:
         *
         * MaxMonitoredItemsPerCall = 100
         *
         * Therefore we use at most 100 MonitoredItems per Subscription.
         *
         * Example with 118 tags:
         *
         * Subscription 1 -> 100 items
         * Subscription 2 -> 18 items
         */
        private readonly List<Subscription> m_subscriptions =
            new List<Subscription>();

        private readonly List<RowInfo> m_rows =
            new List<RowInfo>();

        private readonly Dictionary<uint, RowInfo> m_rowsByClientHandle =
            new Dictionary<uint, RowInfo>();

        private MonitoredItemNotificationEventHandler m_notificationHandler;

        private bool m_suppressCellEvents;

        /// <summary>
        /// Background color used for the "Value" cell of writable tags.
        /// </summary>
        private static readonly Color WritableCellColor =
            Color.LightGoldenrodYellow;

        /// <summary>
        /// Maximum number of nodes/items sent in a single service call
        /// during initial reading/creation.
        ///
        /// The Parker server reports limits of 100, but using 20 here
        /// provides an additional safety margin.
        /// </summary>
        private const int MaxOperationsPerRequest = 20;

        /// <summary>
        /// Maximum number of MonitoredItems in one Subscription.
        ///
        /// Parker reports:
        /// MaxMonitoredItemsPerCall = 100
        ///
        /// Keeping each Subscription at 100 items or less also prevents
        /// ApplyChanges() from generating a ModifyMonitoredItems request
        /// containing more than 100 items.
        /// </summary>
        private const int MaxMonitoredItemsPerSubscription = 100;

        #endregion

        #region Private Helper Class

        private class RowInfo
        {
            public string RawTag;
            public string ShortName;
            public NodeId NodeId;
            public bool ParseOk;
            public bool Writable;
            public BuiltInType DataType = BuiltInType.String;
            public MonitoredItem Item;
            public int RowIndex;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Sets up the dedicated subscriptions for the given tags and fills the grid.
        /// Must be called before ShowDialog().
        /// </summary>
        public void Initialize(
            Session session,
            List<string> rawTags,
            int defaultNamespaceIndex)
        {
            m_session = session;

            m_notificationHandler =
                new MonitoredItemNotificationEventHandler(
                    MonitoredItem_Notification);

            // ------------------------------------------------------------
            // Build the rows and resolve the NodeIds.
            // ------------------------------------------------------------

            foreach (string rawTag in rawTags)
            {
                RowInfo row = new RowInfo();

                row.RawTag = rawTag;
                row.ShortName = BuildShortName(rawTag);

                string fullIdentifier =
                    rawTag.StartsWith(
                        "ns=",
                        StringComparison.OrdinalIgnoreCase)
                    ? rawTag
                    : Utils.Format(
                        "ns={0};s={1}",
                        defaultNamespaceIndex,
                        rawTag);

                try
                {
                    row.NodeId = NodeId.Parse(fullIdentifier);
                    row.ParseOk = true;
                }
                catch (Exception)
                {
                    row.ParseOk = false;
                }

                m_rows.Add(row);
            }

            // ------------------------------------------------------------
            // Batch read AccessLevel, UserAccessLevel and Value.
            // ------------------------------------------------------------

            List<RowInfo> readableRows =
                new List<RowInfo>();

            foreach (RowInfo row in m_rows)
            {
                if (row.ParseOk)
                {
                    readableRows.Add(row);
                }
            }

            List<DataValue> results =
                new List<DataValue>();

            for (
                int chunkStart = 0;
                chunkStart < readableRows.Count;
                chunkStart += MaxOperationsPerRequest)
            {
                int chunkCount =
                    Math.Min(
                        MaxOperationsPerRequest,
                        readableRows.Count - chunkStart);

                ReadValueIdCollection nodesToRead =
                    new ReadValueIdCollection();

                for (
                    int ii = chunkStart;
                    ii < chunkStart + chunkCount;
                    ii++)
                {
                    RowInfo row = readableRows[ii];

                    nodesToRead.Add(
                        new ReadValueId()
                        {
                            NodeId = row.NodeId,
                            AttributeId = Attributes.AccessLevel
                        });

                    nodesToRead.Add(
                        new ReadValueId()
                        {
                            NodeId = row.NodeId,
                            AttributeId = Attributes.UserAccessLevel
                        });

                    nodesToRead.Add(
                        new ReadValueId()
                        {
                            NodeId = row.NodeId,
                            AttributeId = Attributes.Value
                        });
                }

                DataValueCollection chunkResults = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                m_session.Read(
                    null,
                    0,
                    TimestampsToReturn.Neither,
                    nodesToRead,
                    out chunkResults,
                    out diagnosticInfos);

                if (chunkResults != null)
                {
                    results.AddRange(chunkResults);
                }
            }

            // ------------------------------------------------------------
            // Create the grid rows and MonitoredItems.
            //
            // IMPORTANT:
            // A new Subscription is created every 100 MonitoredItems.
            // ------------------------------------------------------------

            m_suppressCellEvents = true;

            Subscription currentSubscription = null;
            int itemsInCurrentSubscription = 0;

            for (int ii = 0; ii < readableRows.Count; ii++)
            {
                RowInfo row = readableRows[ii];

                // --------------------------------------------------------
                // Start a new Subscription when:
                //
                // 1) there is no current Subscription
                // 2) the current one already contains 100 items
                // --------------------------------------------------------

                if (
                    currentSubscription == null ||
                    itemsInCurrentSubscription >=
                        MaxMonitoredItemsPerSubscription)
                {
                    currentSubscription =
                        CreateSubscription();

                    itemsInCurrentSubscription = 0;

                    Console.WriteLine(
                        "Created new OPC UA Subscription. " +
                        $"Total subscriptions = {m_subscriptions.Count}");
                }

                // --------------------------------------------------------
                // Decode AccessLevel/UserAccessLevel/Value.
                // --------------------------------------------------------

                int resultIndex = ii * 3;

                byte accessLevel = 0;
                byte userAccessLevel = 0;

                if (
                    resultIndex < results.Count &&
                    !ServiceResult.IsBad(
                        results[resultIndex].StatusCode))
                {
                    accessLevel =
                        Convert.ToByte(
                            results[resultIndex].Value);
                }

                if (
                    resultIndex + 1 < results.Count &&
                    !ServiceResult.IsBad(
                        results[resultIndex + 1].StatusCode))
                {
                    userAccessLevel =
                        Convert.ToByte(
                            results[resultIndex + 1].Value);
                }

                row.Writable =
                    ((accessLevel & AccessLevels.CurrentWrite) != 0) &&
                    ((userAccessLevel & AccessLevels.CurrentWrite) != 0);

                DataValue currentValue = null;

                if (resultIndex + 2 < results.Count)
                {
                    currentValue = results[resultIndex + 2];
                }

                string valueText = String.Empty;
                string qualityText = String.Empty;

                if (currentValue != null)
                {
                    if (
                        !ServiceResult.IsBad(
                            currentValue.StatusCode) &&
                        currentValue.Value != null)
                    {
                        row.DataType =
                            TypeInfo.Construct(
                                currentValue.Value).BuiltInType;
                    }

                    valueText =
                        Utils.Format(
                            "{0}",
                            currentValue.WrappedValue);

                    qualityText =
                        Utils.Format(
                            "{0}",
                            currentValue.StatusCode);
                }

                // --------------------------------------------------------
                // Create MonitoredItem.
                // --------------------------------------------------------

                MonitoredItem monitoredItem =
                    new MonitoredItem(
                        currentSubscription.DefaultItem);

                monitoredItem.StartNodeId = row.NodeId;
                monitoredItem.AttributeId = Attributes.Value;
                monitoredItem.DisplayName = row.ShortName;
                monitoredItem.MonitoringMode =
                    MonitoringMode.Reporting;

                monitoredItem.SamplingInterval =
                    (int)ScanMsNUD.Value;

                monitoredItem.QueueSize = 0;
                monitoredItem.DiscardOldest = true;

                monitoredItem.Notification +=
                    m_notificationHandler;

                currentSubscription.AddItem(
                    monitoredItem);

                row.Item = monitoredItem;

                AddGridRow(
                    row,
                    valueText,
                    qualityText,
                    String.Empty);

                itemsInCurrentSubscription++;

                // --------------------------------------------------------
                // Send CreateMonitoredItems periodically.
                //
                // We use batches of MaxOperationsPerRequest = 20.
                //
                // This means that even inside a 100-item Subscription,
                // creation is sent as:
                //
                // 20 + 20 + 20 + 20 + 20
                //
                // instead of one 100-item request.
                // --------------------------------------------------------

                bool isLastRow =
                    (ii == readableRows.Count - 1);

                bool chunkIsFull =
                    (itemsInCurrentSubscription %
                     MaxOperationsPerRequest) == 0;

                if (chunkIsFull || isLastRow)
                {
                    Console.WriteLine(
                        "ApplyChanges() for Subscription " +
                        $"{m_subscriptions.IndexOf(currentSubscription) + 1} " +
                        $"with {itemsInCurrentSubscription} item(s).");

                    currentSubscription.ApplyChanges();

                    Console.WriteLine(
                        "ApplyChanges() OK.");
                }
            }

            // ------------------------------------------------------------
            // Rows whose NodeId could not be parsed.
            // ------------------------------------------------------------

            foreach (RowInfo row in m_rows)
            {
                if (!row.ParseOk)
                {
                    AddGridRow(
                        row,
                        String.Empty,
                        "Invalid NodeId",
                        String.Empty);
                }
            }

            // ------------------------------------------------------------
            // Record ClientHandle -> RowInfo mapping.
            // ------------------------------------------------------------

            foreach (RowInfo row in m_rows)
            {
                if (row.Item != null)
                {
                    monitoredItem_Handle(row);

                    if (ServiceResult.IsBad(row.Item.Status.Error))
                    {
                        SetRowText(
                            row.RowIndex,
                            QualityCol.Index,
                            row.Item.Status.Error.StatusCode.ToString());
                    }
                }
            }

            m_suppressCellEvents = false;

            // ------------------------------------------------------------
            // Information label.
            // ------------------------------------------------------------

            string serverUrl =
                (m_session.ConfiguredEndpoint != null)
                ? m_session.ConfiguredEndpoint.EndpointUrl.ToString()
                : "";

            InfoLB.Text =
                Utils.Format(
                    "Connected anonymously to {0} - {1} tag(s) loaded - {2} subscription(s).",
                    serverUrl,
                    m_rows.Count,
                    m_subscriptions.Count);
        }

        #endregion

        #region Subscription Management

        /// <summary>
        /// Creates one dedicated Subscription.
        /// Maximum number of MonitoredItems per Subscription is
        /// MaxMonitoredItemsPerSubscription.
        /// </summary>
        private Subscription CreateSubscription()
        {
            Subscription subscription =
                new Subscription(
                    m_session.DefaultSubscription);

            subscription.PublishingEnabled = true;

            subscription.PublishingInterval =
                (int)ScanMsNUD.Value;

            subscription.KeepAliveCount = 10;
            subscription.LifetimeCount = 10;
            subscription.MaxNotificationsPerPublish = 1000;
            subscription.Priority = 100;

            m_session.AddSubscription(
                subscription);

            subscription.Create();

            m_subscriptions.Add(
                subscription);

            return subscription;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Keeps only the last two dot-separated segments of the raw tag.
        /// </summary>
        private static string BuildShortName(string rawTag)
        {
            if (String.IsNullOrEmpty(rawTag))
            {
                return rawTag;
            }

            string[] parts =
                rawTag.Split('.');

            if (parts.Length <= 2)
            {
                return rawTag;
            }

            return
                parts[parts.Length - 2] +
                "." +
                parts[parts.Length - 1];
        }

        private void monitoredItem_Handle(RowInfo row)
        {
            row.Item.Handle = row;

            m_rowsByClientHandle[
                row.Item.ClientHandle] = row;
        }

        private void AddGridRow(
            RowInfo row,
            string valueText,
            string qualityText,
            string updatedText)
        {
            int rowIndex =
                ItemsGV.Rows.Add(
                    row.ShortName,
                    valueText,
                    qualityText,
                    updatedText);

            row.RowIndex = rowIndex;

            DataGridViewRow gridRow =
                ItemsGV.Rows[rowIndex];

            gridRow.Tag = row;

            if (
                row.ParseOk &&
                row.Writable)
            {
                gridRow.Cells[
                    ValueCol.Index].Style.BackColor =
                    WritableCellColor;

                gridRow.Cells[
                    ValueCol.Index].ReadOnly = false;
            }
            else
            {
                gridRow.Cells[
                    ValueCol.Index].ReadOnly = true;
            }
        }

        private void SetRowText(
            int rowIndex,
            int columnIndex,
            string text)
        {
            if (
                rowIndex < 0 ||
                rowIndex >= ItemsGV.Rows.Count)
            {
                return;
            }

            ItemsGV.Rows[
                rowIndex].Cells[
                columnIndex].Value = text;
        }

        /// <summary>
        /// Converts the text typed by the user into the data type
        /// currently held by the tag.
        /// </summary>
        private static object ChangeType(
            string text,
            BuiltInType builtInType)
        {
            switch (builtInType)
            {
                case BuiltInType.Boolean:
                    return Convert.ToBoolean(text);

                case BuiltInType.SByte:
                    return Convert.ToSByte(text);

                case BuiltInType.Byte:
                    return Convert.ToByte(text);

                case BuiltInType.Int16:
                    return Convert.ToInt16(text);

                case BuiltInType.UInt16:
                    return Convert.ToUInt16(text);

                case BuiltInType.Int32:
                    return Convert.ToInt32(text);

                case BuiltInType.UInt32:
                    return Convert.ToUInt32(text);

                case BuiltInType.Int64:
                    return Convert.ToInt64(text);

                case BuiltInType.UInt64:
                    return Convert.ToUInt64(text);

                case BuiltInType.Float:
                    return Convert.ToSingle(text);

                case BuiltInType.Double:
                    return Convert.ToDouble(text);

                default:
                    return text;
            }
        }

        /// <summary>
        /// Updates the display with a new value for a monitored variable.
        /// </summary>
        private void MonitoredItem_Notification(
            MonitoredItem monitoredItem,
            MonitoredItemNotificationEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(
                    new MonitoredItemNotificationEventHandler(
                        MonitoredItem_Notification),
                    monitoredItem,
                    e);

                return;
            }

            try
            {
                if (
                    this.IsDisposed ||
                    m_session == null)
                {
                    return;
                }

                MonitoredItemNotification notification =
                    e.NotificationValue as
                    MonitoredItemNotification;

                if (notification == null)
                {
                    return;
                }

                RowInfo row =
                    monitoredItem.Handle as RowInfo;

                if (
                    row == null ||
                    row.RowIndex < 0 ||
                    row.RowIndex >= ItemsGV.Rows.Count)
                {
                    return;
                }

                // Don't overwrite a cell the user is currently typing into.
                if (
                    ItemsGV.IsCurrentCellInEditMode &&
                    ItemsGV.CurrentCell != null &&
                    ItemsGV.CurrentCell.RowIndex ==
                        row.RowIndex)
                {
                    return;
                }

                m_suppressCellEvents = true;

                DataGridViewRow gridRow =
                    ItemsGV.Rows[row.RowIndex];

                gridRow.Cells[
                    ValueCol.Index].Value =
                    Utils.Format(
                        "{0}",
                        notification.Value.WrappedValue);

                gridRow.Cells[
                    QualityCol.Index].Value =
                    Utils.Format(
                        "{0}",
                        notification.Value.StatusCode);

                gridRow.Cells[
                    UpdatedCol.Index].Value =
                    Utils.Format(
                        "{0:HH:mm:ss.fff}",
                        notification.Value.SourceTimestamp
                            .ToLocalTime());

                m_suppressCellEvents = false;
            }
            catch (Exception exception)
            {
                m_suppressCellEvents = false;

                ClientUtils.HandleException(
                    this.Text,
                    exception);
            }
        }

        #endregion

        #region Event Handlers

        private void ItemsGV_CellBeginEdit(
            object sender,
            DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex != ValueCol.Index)
            {
                e.Cancel = true;
                return;
            }

            RowInfo row =
                ItemsGV.Rows[
                    e.RowIndex].Tag as RowInfo;

            if (
                row == null ||
                !row.ParseOk ||
                !row.Writable ||
                row.Item == null)
            {
                e.Cancel = true;
            }
        }

        private void ItemsGV_CellEndEdit(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (
                m_suppressCellEvents ||
                e.ColumnIndex != ValueCol.Index)
            {
                return;
            }

            RowInfo row =
                ItemsGV.Rows[
                    e.RowIndex].Tag as RowInfo;

            if (
                row == null ||
                !row.ParseOk ||
                !row.Writable ||
                row.Item == null)
            {
                return;
            }

            try
            {
                string text =
                    Convert.ToString(
                        ItemsGV.Rows[
                            e.RowIndex].Cells[
                            ValueCol.Index].Value);

                WriteValue valueToWrite =
                    new WriteValue();

                valueToWrite.NodeId =
                    row.NodeId;

                valueToWrite.AttributeId =
                    Attributes.Value;

                valueToWrite.Value.Value =
                    ChangeType(
                        text,
                        row.DataType);

                valueToWrite.Value.StatusCode =
                    StatusCodes.Good;

                valueToWrite.Value.ServerTimestamp =
                    DateTime.MinValue;

                valueToWrite.Value.SourceTimestamp =
                    DateTime.MinValue;

                WriteValueCollection valuesToWrite =
                    new WriteValueCollection();

                valuesToWrite.Add(
                    valueToWrite);

                StatusCodeCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                m_session.Write(
                    null,
                    valuesToWrite,
                    out results,
                    out diagnosticInfos);

                ClientBase.ValidateResponse(
                    results,
                    valuesToWrite);

                ClientBase.ValidateDiagnosticInfos(
                    diagnosticInfos,
                    valuesToWrite);

                if (StatusCode.IsBad(results[0]))
                {
                    throw new ServiceResultException(
                        results[0]);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(
                    "Error writing value",
                    exception);
            }
        }

        private void ApplyScanBTN_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                if (m_subscriptions.Count == 0)
                {
                    return;
                }

                int newInterval =
                    (int)ScanMsNUD.Value;

                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "=== APPLY SCAN ===");

                Console.WriteLine(
                    $"New interval       = {newInterval} ms");

                Console.WriteLine(
                    $"Rows               = {m_rows.Count}");

                Console.WriteLine(
                    $"Subscriptions      = {m_subscriptions.Count}");

                Console.WriteLine(
                    $"Max items/subscr.  = {MaxMonitoredItemsPerSubscription}");

                // --------------------------------------------------------
                // Apply the new interval independently to every
                // Subscription.
                //
                // With 118 items this becomes:
                //
                // Subscription 1 -> 100 ModifyMonitoredItems
                // Subscription 2 ->  18 ModifyMonitoredItems
                //
                // Therefore the Parker limit of 100 is never exceeded.
                // --------------------------------------------------------

                foreach (
                    Subscription subscription
                    in m_subscriptions)
                {
                    int itemCount =
                        subscription.MonitoredItems.Count();

                    Console.WriteLine();
                    Console.WriteLine(
                        $"Subscription {subscription.Id}");

                    Console.WriteLine(
                        $"MonitoredItems = {itemCount}");

                    subscription.PublishingInterval =
                        newInterval;

                    foreach (
                        MonitoredItem item
                        in subscription.MonitoredItems)
                    {
                        item.SamplingInterval =
                            newInterval;
                    }

                    Console.WriteLine(
                        "Calling ApplyChanges()...");

                    subscription.ApplyChanges();

                    Console.WriteLine(
                        $"Subscription {subscription.Id}: " +
                        "ApplyChanges() OK");
                }

                Console.WriteLine();
                Console.WriteLine(
                    "=== APPLY SCAN OK ===");

                Console.WriteLine(
                    "========================================");
                Console.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "=== APPLY SCAN ERROR ===");

                Console.WriteLine(
                    exception.ToString());

                Console.WriteLine(
                    "========================================");
                Console.WriteLine();

                ClientUtils.HandleException(
                    this.Text,
                    exception);
            }
        }

        private void CloseBTN_Click(
            object sender,
            EventArgs e)
        {
            this.Close();
        }

        private void MonitoredTableDlg_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            try
            {
                if (
                    m_session != null &&
                    m_session.Connected)
                {
                    // Delete every Subscription created by this window.
                    foreach (
                        Subscription subscription
                        in m_subscriptions)
                    {
                        try
                        {
                            subscription.Delete(true);
                        }
                        catch (Exception)
                        {
                            // Best effort cleanup.
                        }

                        try
                        {
                            m_session.RemoveSubscription(
                                subscription);
                        }
                        catch (Exception)
                        {
                            // Best effort cleanup.
                        }
                    }
                }

                m_subscriptions.Clear();
            }
            catch (Exception)
            {
                // Best effort cleanup.
            }
        }

        #endregion
    }
}