/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace Quickstarts.DataAccessClient
{
    /// <summary>
    /// The main form for a simple Data Access Client application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Constructors
        /// <summary>
        /// Creates an empty form.
        /// </summary>
        private MainForm()
        {
            InitializeComponent();
            this.Icon = ClientUtils.GetAppIcon();
        }

        /// <summary>
        /// Creates a form which uses the specified client configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        public MainForm(ApplicationConfiguration configuration)
        {
            InitializeComponent();
            this.Icon = ClientUtils.GetAppIcon();
            ConnectServerCTRL.Configuration = m_configuration = configuration;
            if (ConnectServerCTRL.ServerUrl == "")
            {
                //ConnectServerCTRL.UserIdentity = new UserIdentity("Felix", "Felix");
                ConnectServerCTRL.ServerUrl = "opc.tcp://192.168.1.116:4840";
            }
            this.Text = "Simply_Parker_OPCUA_Explorer_2";
            // create the callback.
            m_MonitoredItem_Notification = new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
        }
        #endregion

        #region Private Fields
        private ApplicationConfiguration m_configuration;
        private Session m_session;
        private bool m_connectedOnce;
        private Subscription m_subscription;
        private MonitoredItemNotificationEventHandler m_MonitoredItem_Notification;
        public Rootobject questions ;
        public string RootDirOPCUA ;

        /// <summary>
        /// Namespace index used to build the full NodeId of the tags listed in
        /// OPCUA_Config_Monitor_Table.txt, which only contains the "|var|..." identifier
        /// without the "ns=x;s=" prefix. Change this if your CODESYS symbols live in a
        /// different namespace (check an existing monitored item's NodeId, e.g. in
        /// OPCUA_Config_Monitor.txt, which already includes the "ns=...;s=" prefix).
        /// </summary>
        private const int MonitorTableDefaultNamespaceIndex = 4;

        #endregion

        #region Private Methods
        #endregion

        #region Event Handlers
        /// <summary>
        /// Connects to a server.
        /// </summary>
        private void Server_ConnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Connect();

            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Disconnects from the current session.
        /// </summary>
        private void Server_DisconnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Disconnect();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Prompts the user to choose a server on another host.
        /// </summary>
        private void Server_DiscoverMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Discover(null);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after connecting to or disconnecting from the server.
        /// </summary>
        private void Server_ConnectComplete(object sender, EventArgs e)
        {
            try
            {
                //ConnectServerCTRL.UserIdentity = new UserIdentity("Felix", "felixgi1");
                m_session = ConnectServerCTRL.Session;

                if (m_session == null)
                {
                    MonitoredItemsLV.Items.Clear();
                    BrowseNodesTV.Nodes.Clear();
                    BrowseNodesTV.Enabled = false;
                    MonitoredItemsLV.Enabled = false;
                    return;
                }

                // set a suitable initial state.
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                }

                // populate the browse view.
                PopulateBranch(ObjectIds.ObjectsFolder, BrowseNodesTV.Nodes);

                BrowseNodesTV.Enabled = true;
                MonitoredItemsLV.Enabled = true;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after a communicate error was detected.
        /// </summary>
        private void Server_ReconnectStarting(object sender, EventArgs e)
        {
            try
            {
                BrowseNodesTV.Enabled = false;
                MonitoredItemsLV.Enabled = false;
                AttributesLV.Items.Clear();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after reconnecting to the server.
        /// </summary>
        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;

                foreach (Subscription subscription in m_session.Subscriptions)
                {
                    m_subscription = subscription;
                    break;
                }

                BrowseNodesTV.Enabled = true;
                MonitoredItemsLV.Enabled = true;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Cleans up when the main form closes.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var process1 in Process.GetProcessesByName("AasxServerWindows"))
            {
                process1.Kill();
            }
            ConnectServerCTRL.Disconnect();
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Populates the branch in the tree view.
        /// </summary>
        /// <param name="sourceId">The NodeId of the Node to browse.</param>
        /// <param name="nodes">The node collect to populate.</param>
        private void PopulateBranch(NodeId sourceId, TreeNodeCollection nodes)
        {
            try
            {
                nodes.Clear();

                // find all of the components of the node.
                BrowseDescription nodeToBrowse1 = new BrowseDescription();

                nodeToBrowse1.NodeId = sourceId;
                nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
                nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.Aggregates;
                nodeToBrowse1.IncludeSubtypes = true;
                nodeToBrowse1.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
                nodeToBrowse1.ResultMask = (uint)BrowseResultMask.All;
                
                // find all nodes organized by the node.
                BrowseDescription nodeToBrowse2 = new BrowseDescription();

                nodeToBrowse2.NodeId = sourceId;
                nodeToBrowse2.BrowseDirection = BrowseDirection.Forward;
                nodeToBrowse2.ReferenceTypeId = ReferenceTypeIds.Organizes;
                nodeToBrowse2.IncludeSubtypes = true;
                nodeToBrowse2.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
                nodeToBrowse2.ResultMask = (uint)BrowseResultMask.All;

                BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection();
                nodesToBrowse.Add(nodeToBrowse1);
                nodesToBrowse.Add(nodeToBrowse2);

                // fetch references from the server.
                ReferenceDescriptionCollection references = FormUtils.Browse(m_session, nodesToBrowse, false);
                
                // process results.
                for (int ii = 0; ii < references.Count; ii++)
                {
                    ReferenceDescription target = references[ii];

                    // add node.
                    TreeNode child = new TreeNode(Utils.Format("{0}", target));
                    child.Tag = target;
                    child.Nodes.Add(new TreeNode());
                    nodes.Add(child);
                }

                // update the attributes display.
                DisplayAttributes(sourceId);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Displays the attributes and properties in the attributes view.
        /// </summary>
        /// <param name="sourceId">The NodeId of the Node to browse.</param>
        private void DisplayAttributes(NodeId sourceId)
        {
            try
            {
                AttributesLV.Items.Clear();

                ReadValueIdCollection nodesToRead = new ReadValueIdCollection();

                // attempt to read all possible attributes.
                for (uint ii = Attributes.NodeClass; ii <= Attributes.UserExecutable; ii++)
                {
                    ReadValueId nodeToRead = new ReadValueId();
                    nodeToRead.NodeId = sourceId;
                    nodeToRead.AttributeId = ii;
                    nodesToRead.Add(nodeToRead);
                }

                int startOfProperties = nodesToRead.Count;

                // find all of the pror of the node.
                BrowseDescription nodeToBrowse1 = new BrowseDescription();

                nodeToBrowse1.NodeId = sourceId;
                nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
                nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                nodeToBrowse1.IncludeSubtypes = true;
                nodeToBrowse1.NodeClassMask = 0;
                nodeToBrowse1.ResultMask = (uint)BrowseResultMask.All;

                BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection();
                nodesToBrowse.Add(nodeToBrowse1);

                // fetch property references from the server.
                ReferenceDescriptionCollection references = FormUtils.Browse(m_session, nodesToBrowse, false);

                if (references == null)
                {
                    return;
                }

                for (int ii = 0; ii < references.Count; ii++)
                {
                    // ignore external references.
                    if (references[ii].NodeId.IsAbsolute)
                    {
                        continue;
                    }

                    ReadValueId nodeToRead = new ReadValueId();
                    nodeToRead.NodeId = (NodeId)references[ii].NodeId;
                    nodeToRead.AttributeId = Attributes.Value;
                    nodesToRead.Add(nodeToRead);
                }

                // read all values.
                DataValueCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                m_session.Read(
                    null,
                    0,
                    TimestampsToReturn.Neither,
                    nodesToRead,
                    out results,
                    out diagnosticInfos);

                ClientBase.ValidateResponse(results, nodesToRead);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

                // process results.
                for (int ii = 0; ii < results.Count; ii++)
                {
                    string name = null;
                    string datatype = null;
                    string value = null;

                    // process attribute value.
                    if (ii < startOfProperties)
                    {
                        // ignore attributes which are invalid for the node.
                        if (results[ii].StatusCode == StatusCodes.BadAttributeIdInvalid)
                        {
                            continue;
                        }

                        // get the name of the attribute.
                        name = Attributes.GetBrowseName(nodesToRead[ii].AttributeId);

                        // display any unexpected error.
                        if (StatusCode.IsBad(results[ii].StatusCode))
                        {
                            datatype = Utils.Format("{0}", Attributes.GetDataTypeId(nodesToRead[ii].AttributeId));
                            value = Utils.Format("{0}", results[ii].StatusCode);
                        }

                        // display the value.
                        else
                        {
                            TypeInfo typeInfo = TypeInfo.Construct(results[ii].Value);

                            datatype = typeInfo.BuiltInType.ToString();

                            if (typeInfo.ValueRank >= ValueRanks.OneOrMoreDimensions)
                            {
                                datatype += "[]";
                            }

                            value = Utils.Format("{0}", results[ii].Value);
                        }
                    }

                    // process property value.
                    else
                    {
                        // ignore properties which are invalid for the node.
                        if (results[ii].StatusCode == StatusCodes.BadNodeIdUnknown)
                        {
                            continue;
                        }

                        // get the name of the property.
                        name = Utils.Format("{0}", references[ii-startOfProperties]);

                        // display any unexpected error.
                        if (StatusCode.IsBad(results[ii].StatusCode))
                        {
                            datatype = String.Empty;
                            value = Utils.Format("{0}", results[ii].StatusCode);
                        }

                        // display the value.
                        else
                        {
                            TypeInfo typeInfo = TypeInfo.Construct(results[ii].Value);

                            datatype = typeInfo.BuiltInType.ToString();

                            if (typeInfo.ValueRank >= ValueRanks.OneOrMoreDimensions)
                            {
                                datatype += "[]";
                            }

                            value = Utils.Format("{0}", results[ii].Value);
                        }
                    }

                    // add the attribute name/value to the list view.
                    ListViewItem item = new ListViewItem(name);
                    item.SubItems.Add(datatype);
                    item.SubItems.Add(value);
                    AttributesLV.Items.Add(item);
                }

                // adjust width of all columns.
                for (int ii = 0; ii < AttributesLV.Columns.Count; ii++)
                {
                    AttributesLV.Columns[ii].Width = -2;
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Converts a monitoring filter to text for display.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The deadback formatted as a string.</returns>
        private string DeadbandFilterToText(MonitoringFilter filter)
        {
            DataChangeFilter datachangeFilter = filter as DataChangeFilter;

            if (datachangeFilter != null)
            {
                if (datachangeFilter.DeadbandType == (uint)DeadbandType.Absolute)
                {
                    return Utils.Format("{0:##.##}", datachangeFilter.DeadbandValue);
                }

                if (datachangeFilter.DeadbandType == (uint)DeadbandType.Percent)
                {
                    return Utils.Format("{0:##.##}%", datachangeFilter.DeadbandValue);
                }
            }

            return "None";
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the Click event of the Help_ContentsMI control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Help_ContentsMI_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start( Path.GetDirectoryName(Application.ExecutablePath) + "\\Help\\daclientoverview.htm");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to launch help documentation. Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Fetches the children for a node the first time the node is expanded in the tree view.
        /// </summary>
        private void BrowseNodesTV_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                // check if node has already been expanded once.
                if (e.Node.Nodes.Count != 1 || e.Node.Nodes[0].Text != String.Empty)
                {
                    return;
                }

                // get the source for the node.
                ReferenceDescription reference = e.Node.Tag as ReferenceDescription;

                if (reference == null || reference.NodeId.IsAbsolute)
                {
                    e.Cancel = true;
                    return;
                }

                // populate children.
                PopulateBranch((NodeId)reference.NodeId, e.Node.Nodes);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the display after a node is selected.
        /// </summary>
        private void BrowseNodesTV_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                // get the source for the node.
                ReferenceDescription reference = e.Node.Tag as ReferenceDescription;

                if (reference == null || reference.NodeId.IsAbsolute)
                {
                    return;
                }

                // populate children.
                PopulateBranch((NodeId)reference.NodeId, e.Node.Nodes);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Ensures the correct node is selected before displaying the context menu.
        /// </summary>
        private void BrowseNodesTV_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                BrowseNodesTV.SelectedNode = BrowseNodesTV.GetNodeAt(e.X, e.Y);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        
        /// <summary>
        /// Handles the Click event of the Browse_MonitorMI control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Browse_MonitorMI_Click(object sender, EventArgs e)
        {
            try
            {  
                // check if operation is currently allowed.
                if (m_session == null || BrowseNodesTV.SelectedNode == null)
                {
                    return;
                }

                // can only subscribe to local variables. 
                ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;

                if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
                {
                    return;
                }

                ListViewItem item = CreateMonitoredItem((NodeId)reference.NodeId, Utils.Format("{0}", reference));

                m_subscription.ApplyChanges();

                MonitoredItem monitoredItem = (MonitoredItem)item.Tag;
                                
                if (ServiceResult.IsBad(monitoredItem.Status.Error))
                {
                    item.SubItems[8].Text = monitoredItem.Status.Error.StatusCode.ToString();
                }

                item.SubItems.Add(monitoredItem.DisplayName);
                item.SubItems[1].Text = monitoredItem.MonitoringMode.ToString();
                item.SubItems[2].Text = monitoredItem.SamplingInterval.ToString();
                item.SubItems[3].Text = DeadbandFilterToText(monitoredItem.Filter);
                
                MonitoredItemsLV.Columns[0].Width = -2;
                MonitoredItemsLV.Columns[1].Width = -2;
                MonitoredItemsLV.Columns[8].Width = -2;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Creates the monitored item.
        /// </summary>
        private ListViewItem CreateMonitoredItem(NodeId nodeId, string displayName)
        {
            if (m_subscription == null)
            {
                m_subscription = new Subscription(m_session.DefaultSubscription);

                m_subscription.PublishingEnabled = true;
                m_subscription.PublishingInterval = 1000;
                m_subscription.KeepAliveCount = 10;
                m_subscription.LifetimeCount = 10;
                m_subscription.MaxNotificationsPerPublish = 1000;
                m_subscription.Priority = 100;

                m_session.AddSubscription(m_subscription);

                m_subscription.Create();
            }

            // add the new monitored item.
            MonitoredItem monitoredItem = new MonitoredItem(m_subscription.DefaultItem);

            monitoredItem.StartNodeId = nodeId;
            monitoredItem.AttributeId = Attributes.Value;
            monitoredItem.DisplayName = displayName;
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;
            monitoredItem.SamplingInterval = 1000;
            monitoredItem.QueueSize = 0;
            monitoredItem.DiscardOldest = true;
            monitoredItem.Notification += m_MonitoredItem_Notification;

            m_subscription.AddItem(monitoredItem);

            // add the attribute name/value to the list view.
            ListViewItem item = new ListViewItem(monitoredItem.ClientHandle.ToString());
            monitoredItem.Handle = item;

            item.SubItems.Add(monitoredItem.DisplayName);
            item.SubItems.Add(monitoredItem.MonitoringMode.ToString());
            item.SubItems.Add(monitoredItem.SamplingInterval.ToString());
            item.SubItems.Add(DeadbandFilterToText(monitoredItem.Filter));
            item.SubItems.Add(String.Empty);
            item.SubItems.Add(String.Empty);
            item.SubItems.Add(String.Empty);
            item.SubItems.Add(String.Empty);
            item.SubItems.Add(monitoredItem.ResolvedNodeId.Identifier.ToString());

            item.Tag = monitoredItem;
            MonitoredItemsLV.Items.Add(item);

            if (ServiceResult.IsBad(monitoredItem.Status.Error))
            {
                item.SubItems[8].Text = monitoredItem.Status.Error.StatusCode.ToString();
            }
            
            return item;
        }

        /// <summary>
        /// Prompts the use to write the value of a varible.
        /// </summary>
        private void Browse_WriteMI_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || BrowseNodesTV.SelectedNode == null)
                {
                    return;
                }

                // can only subscribe to local variables. 
                ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;

                if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
                {
                    return;
                }

                new WriteValueDlg().ShowDialog(m_session, (NodeId)reference.NodeId, Attributes.Value);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        
        /// <summary>
        /// Handles the Click event of the Browse_ReadHistoryMI control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Browse_ReadHistoryMI_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || BrowseNodesTV.SelectedNode == null)
                {
                    return;
                }

                // can only subscribe to local variables. 
                ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;

                if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
                {
                    return;
                }

                new ReadHistoryDlg().ShowDialog(m_session, (NodeId)reference.NodeId);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the display with a new value for a monitored variable. 
        /// </summary>
        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MonitoredItemNotificationEventHandler(MonitoredItem_Notification), monitoredItem, e);
                return;
            }

            try
            {
                if (m_session == null)
                {
                    return;
                }

                MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;

                if (notification == null)
                {
                    return;
                }

                ListViewItem item = (ListViewItem)monitoredItem.Handle;
                item.SubItems[1].Text = monitoredItem.ResolvedNodeId.ToString();
                item.SubItems[5].Text = Utils.Format("{0}", notification.Value.WrappedValue);
                item.SubItems[6].Text = Utils.Format("{0}", notification.Value.StatusCode);
                item.SubItems[7].Text = Utils.Format("{0:HH:mm:ss.fff}", notification.Value.SourceTimestamp.ToLocalTime());
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Changes the monitoring mode for the currently selected monitored items.
        /// </summary>
        private void Monitoring_MonitoringMode_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || m_subscription == null || MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                // determine the monitoring mode being requested.
                MonitoringMode monitoringMode = MonitoringMode.Disabled;

                if (sender == Monitoring_MonitoringMode_ReportingMI)
                {
                    monitoringMode = MonitoringMode.Reporting;
                }

                if (sender == Monitoring_MonitoringMode_SamplingMI)
                {
                    monitoringMode = MonitoringMode.Sampling;
                }

                // update the monitoring mode.
                List<MonitoredItem> itemsToChange = new List<MonitoredItem>();

                for (int ii = 0; ii < MonitoredItemsLV.SelectedItems.Count; ii++)
                {
                    MonitoredItem monitoredItem = MonitoredItemsLV.SelectedItems[ii].Tag as MonitoredItem;

                    if (monitoredItem != null)
                    {
                        itemsToChange.Add(monitoredItem);
                    }
                }

                // apply the changes to the server.
                m_subscription.SetMonitoringMode(monitoringMode, itemsToChange);

                // update the display.
                for (int ii = 0; ii < itemsToChange.Count; ii++)
                {
                    ListViewItem item = itemsToChange[ii].Handle as ListViewItem;

                    if (item != null)
                    {
                        item.SubItems[8].Text = String.Empty;

                        if (ServiceResult.IsBad(itemsToChange[ii].Status.Error))
                        {
                            item.SubItems[8].Text = itemsToChange[ii].Status.Error.StatusCode.ToString();
                        }

                        item.SubItems[2].Text = itemsToChange[ii].Status.MonitoringMode.ToString();
                    }
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Changes the sampling interval for the currently selected monitored items.
        /// </summary>
        private void Monitoring_SamplingInterval_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || m_subscription == null || MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                // determine the sampling interval being requested.
                double samplingInterval = 0;

                if (sender == Monitoring_SamplingInterval_1000MI)
                {
                    samplingInterval = 1000;
                }
                else if (sender == Monitoring_SamplingInterval_2500MI)
                {
                    samplingInterval = 2500;
                }
                else if (sender == Monitoring_SamplingInterval_5000MI)
                {
                    samplingInterval = 5000;
                }

                // update the monitoring mode.
                List<MonitoredItem> itemsToChange = new List<MonitoredItem>();

                for (int ii = 0; ii < MonitoredItemsLV.SelectedItems.Count; ii++)
                {
                    MonitoredItem monitoredItem = MonitoredItemsLV.SelectedItems[ii].Tag as MonitoredItem;

                    if (monitoredItem != null)
                    {
                        monitoredItem.SamplingInterval = (int)samplingInterval;
                        itemsToChange.Add(monitoredItem);
                    }
                }

                // apply the changes to the server.
                m_subscription.ApplyChanges();

                // update the display.
                for (int ii = 0; ii < itemsToChange.Count; ii++)
                {
                    ListViewItem item = itemsToChange[ii].Handle as ListViewItem;

                    if (item != null)
                    {
                        item.SubItems[8].Text = String.Empty;

                        if (ServiceResult.IsBad(itemsToChange[ii].Status.Error))
                        {
                            item.SubItems[8].Text = itemsToChange[ii].Status.Error.StatusCode.ToString();
                        }

                        item.SubItems[3].Text = itemsToChange[ii].Status.SamplingInterval.ToString();
                    }
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Changes the deadband for the currently selected monitored items.
        /// </summary>
        private void Monitoring_Deadband_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || m_subscription == null || MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                // determine the filter being requested.
                DataChangeFilter filter = new DataChangeFilter();
                filter.Trigger = DataChangeTrigger.StatusValue;

                if (sender == Monitoring_Deadband_Absolute_5MI)
                {
                    filter.DeadbandType = (uint)DeadbandType.Absolute;
                    filter.DeadbandValue = 5.0;
                }
                else if (sender == Monitoring_Deadband_Absolute_10MI)
                {
                    filter.DeadbandType = (uint)DeadbandType.Absolute;
                    filter.DeadbandValue = 10.0;
                }
                else if (sender == Monitoring_Deadband_Absolute_25MI)
                {
                    filter.DeadbandType = (uint)DeadbandType.Absolute;
                    filter.DeadbandValue = 25.0;
                }
                else if (sender == Monitoring_Deadband_Percentage_1MI)
                {
                    filter.DeadbandType = (uint)DeadbandType.Percent;
                    filter.DeadbandValue = 1.0;
                }
                else if (sender == Monitoring_Deadband_Percentage_5MI)
                {
                    filter.DeadbandType = (uint)DeadbandType.Percent;
                    filter.DeadbandValue = 5.0;
                }
                else if (sender == Monitoring_Deadband_Percentage_10MI)
                {
                    filter.DeadbandType = (uint)DeadbandType.Percent;
                    filter.DeadbandValue = 10.0;
                }
                else
                {
                    filter = null;
                }

                // update the monitoring mode.
                List<MonitoredItem> itemsToChange = new List<MonitoredItem>();

                for (int ii = 0; ii < MonitoredItemsLV.SelectedItems.Count; ii++)
                {
                    MonitoredItem monitoredItem = MonitoredItemsLV.SelectedItems[ii].Tag as MonitoredItem;

                    if (monitoredItem != null)
                    {
                        monitoredItem.Filter = filter;
                        itemsToChange.Add(monitoredItem);
                    }
                }

                // apply the changes to the server.
                m_subscription.ApplyChanges();

                // update the display.
                for (int ii = 0; ii < itemsToChange.Count; ii++)
                {
                    ListViewItem item = itemsToChange[ii].Handle as ListViewItem;

                    if (item != null)
                    {
                        item.SubItems[8].Text = String.Empty;

                        if (ServiceResult.IsBad(itemsToChange[ii].Status.Error))
                        {
                            itemsToChange[ii].Filter = null;
                            item.SubItems[8].Text = itemsToChange[ii].Status.Error.StatusCode.ToString();
                        }

                        item.SubItems[4].Text = DeadbandFilterToText(itemsToChange[ii].Status.Filter);
                    }
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Handles the Click event of the Monitoring_DeleteMI control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Monitoring_DeleteMI_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                // collect the items to delete.
                List<ListViewItem> itemsToDelete = new List<ListViewItem>();

                for (int ii = 0; ii < MonitoredItemsLV.SelectedItems.Count; ii++)
                {
                    MonitoredItem monitoredItem = MonitoredItemsLV.SelectedItems[ii].Tag as MonitoredItem;

                    if (monitoredItem != null)
                    {
                        monitoredItem.Notification -= m_MonitoredItem_Notification;
                        itemsToDelete.Add(MonitoredItemsLV.SelectedItems[ii]);

                        if (m_subscription != null)
                        {
                            m_subscription.RemoveItem(monitoredItem);
                        }
                    }
                }

                // update the server.
                if (m_subscription != null)
                {
                    m_subscription.ApplyChanges();

                    // check the status.
                    for (int ii = 0; ii < itemsToDelete.Count; ii++)
                    {
                        MonitoredItem monitoredItem = itemsToDelete[ii].Tag as MonitoredItem;

                        if (ServiceResult.IsBad(monitoredItem.Status.Error))
                        {
                            itemsToDelete[ii].SubItems[8].Text = monitoredItem.Status.Error.StatusCode.ToString();
                            continue;
                        }
                    }
                }

                // remove the items.
                for (int ii = 0; ii < itemsToDelete.Count; ii++)
                {
                    itemsToDelete[ii].Remove();
                }

                MonitoredItemsLV.Columns[0].Width = -2;
                MonitoredItemsLV.Columns[1].Width = -2;
                MonitoredItemsLV.Columns[8].Width = -2;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        private void Monitoring_DeleteMI_All()
        {
            MonitoredItemsLV.Clear();
        }


        private void BrowsingMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Browse_MonitorMI.Enabled = true;
            Browse_ReadHistoryMI.Enabled = true;
            Browse_WriteMI.Enabled = true;

            if (m_session == null || BrowseNodesTV.SelectedNode == null)
            {
                Browse_MonitorMI.Enabled = false;
                Browse_ReadHistoryMI.Enabled = false;
                Browse_WriteMI.Enabled = false;
                return;
            }

            ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;

            if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
            {
                Browse_MonitorMI.Enabled = false;
                Browse_ReadHistoryMI.Enabled = false;
                Browse_WriteMI.Enabled = false;
                return;
            }
        }

        private void Monitoring_WriteMI_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || m_subscription == null || MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                MonitoredItem monitoredItem = MonitoredItemsLV.SelectedItems[0].Tag as MonitoredItem;

                if (monitoredItem != null)
                {
                    new WriteValueDlg().ShowDialog(m_session, (NodeId)monitoredItem.ResolvedNodeId, Attributes.Value);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Creates monitored items from a saved list of node ids.
        /// </summary>
        private void File_LoadMI_Click(object sender, EventArgs e)
        {
            try
            {
                string FileName = Application.StartupPath + "\\OPCUA_Config_Monitor.txt";
                StreamReader sw = new StreamReader(FileName);
                string dummy = sw.ReadLine();
                int maxi = Int32.Parse(sw.ReadLine());
                //ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;
                ReferenceDescription reference;
                reference = new ReferenceDescription();
                ;
                for (int i = 0; i < maxi; i++)
                {
                reference.NodeId = sw.ReadLine();
                ListViewItem item = CreateMonitoredItem((NodeId)reference.NodeId, Utils.Format("{0}", reference));
                MonitoredItem monitoredItem = (MonitoredItem)item.Tag;
                if (ServiceResult.IsBad(monitoredItem.Status.Error))
                {
                    item.SubItems[8].Text = monitoredItem.Status.Error.StatusCode.ToString();
                }

                item.SubItems.Add(monitoredItem.DisplayName);
                item.SubItems[1].Text = monitoredItem.MonitoringMode.ToString();
                item.SubItems[2].Text = monitoredItem.SamplingInterval.ToString();
                item.SubItems[3].Text = DeadbandFilterToText(monitoredItem.Filter);
                m_subscription.ApplyChanges();
                }
                sw.Close();
                return;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Opens a pop-up window that monitors (and optionally writes) the list of tags
        /// contained in OPCUA_Config_Monitor_Table.txt. Connects automatically and
        /// anonymously, using the server URL shown in the main form, if not already connected.
        /// </summary>
        private void File_LoadMITable_Click(object sender, EventArgs e)
        {
            try
            {
                string FileName = Application.StartupPath + "\\OPCUA_Config_Monitor_Table.txt";

                if (!File.Exists(FileName))
                {
                    MessageBox.Show(
                        this,
                        "File not found:\r\n" + FileName,
                        this.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // read the raw tag list (one "|var|..." identifier per line).
                List<string> tags = new List<string>();

                using (StreamReader sw = new StreamReader(FileName))
                {
                    string line;

                    while ((line = sw.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Length == 0)
                        {
                            continue;
                        }

                        tags.Add(line);
                    }
                }

                if (tags.Count == 0)
                {
                    MessageBox.Show(
                        this,
                        "The file does not contain any tag:\r\n" + FileName,
                        this.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // ensure we have a session, connecting anonymously to the server URL
                // shown in the main form if the user has not already connected.
                if (!EnsureConnectedAnonymous())
                {
                    return;
                }

                using (MonitoredTableDlg dlg = new MonitoredTableDlg())
                {
                    dlg.Initialize(m_session, tags, MonitorTableDefaultNamespaceIndex);
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Makes sure a session towards ConnectServerCTRL.ServerUrl exists, connecting
        /// anonymously and automatically (no user interaction) when necessary.
        /// </summary>
        /// <returns>True if a usable session is available.</returns>
        private bool EnsureConnectedAnonymous()
        {
            if (m_session != null && m_session.Connected)
            {
                return true;
            }

            if (String.IsNullOrEmpty(ConnectServerCTRL.ServerUrl))
            {
                MessageBox.Show(
                    this,
                    "No server URL is set in the main form.",
                    this.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            // force an anonymous login, regardless of any user previously set via "Set User...".
            ConnectServerCTRL.UserIdentity = new UserIdentity();
            ConnectServerCTRL.Connect();

            m_session = ConnectServerCTRL.Session;

            if (m_session == null)
            {
                MessageBox.Show(
                    this,
                    "Unable to connect anonymously to:\r\n" + ConnectServerCTRL.ServerUrl,
                    this.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the current monitored items.
        /// </summary>
        private void File_SaveMI_Click(object sender, EventArgs e)
        {
            try
            {
                string swline;
                int i;
                i = 0;
                string FileName = Application.StartupPath + "\\OPCUA_Config_Monitor.txt";
                StreamWriter sw = new StreamWriter(FileName);
                sw.WriteLine("OPCUA Monitor Configuration");
                sw.WriteLine(MonitoredItemsLV.Items.Count);
                var t = m_subscription;
                foreach (var pippo in MonitoredItemsLV.Items)
                {
                    swline = MonitoredItemsLV.Items[i].SubItems[1].Text;
                    sw.WriteLine(swline);
                    i = i + 1;
                }
                sw.Close();
                return;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Sets the locale to use.
        /// </summary>
        private void Server_SetLocaleMI_Click(object sender, EventArgs e)
        {

            try
            {
                if (m_session == null)
                {
                    return;
                }

                string locale = new SelectLocaleDlg().ShowDialog(m_session);

                if (locale == null)
                {
                    return;
                }

                ConnectServerCTRL.PreferredLocales = new string[] { locale };
                m_session.ChangePreferredLocales(new StringCollection(ConnectServerCTRL.PreferredLocales));
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        private void Server_SetUserMI_Click(object sender, EventArgs e)
        {
            try
            {
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        #endregion

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit the application?", "UA Sample Client", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void ConnectServerCTRL_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*
            int t;
            RESTClientPut rClient = new RESTClientPut();
            string actualport = textBox5.Text;
            string actualShell;
            actualShell = textBox1.Text.Substring (0, textBox1.TextLength-9);
            if (actualport == "")
            {
                actualport = "51310";
            }
            string actualip = textBox4.Text; 
            if (actualip =="")
            {
                actualip = "localhost";
            }
            rClient.endPoint = "http://"+actualip+":"+actualport+"/aas/"+ actualShell + "/elements/";
            if (MonitoredItemsLV.Items.Count==0)
            {
                debugOutput("No Items to send Please check Monitor table");
                return;

            }
            t = 0;
            foreach (var pippo in questions.submodelElements)
            {
                rClient.NameIdVar[t] = pippo.idShort;
                t = t + 1;
            }
            string strJSON = string.Empty;
            for (int i = 0; i < MonitoredItemsLV.Items.Count ; i++)
            {
                rClient.endValue[i] = MonitoredItemsLV.Items[i].SubItems[5].Text;
                if (rClient.endValue[i] == "")
                {
                    rClient.endValue[i] = "0";
                }
                strJSON = rClient.makeRequest(rClient.endValue[i], rClient.NameIdVar[i]);
                debugOutput(strJSON + i);
            }
            debugOutput("SEND ALL Values in Monitor To Server AAS.");
            */
        }
        public void debugOutput(string strDebugText)
        {
            try
            {
                System.Diagnostics.Debug.Write(strDebugText + Environment.NewLine);
                //txtResponse.Text = txtResponse.Text + strDebugText + Environment.NewLine;
                //txtResponse.SelectionStart = txtResponse.TextLength;
                //txtResponse.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message, ToString() + Environment.NewLine);
            }
        }
        class RESTClientGet
        {
            public enum httpVerb
            {
                GET,
                POST,
                PUT,
                DELETE
            }

            public string[] endPoint = new string[100];
            public string[] NameIdVar = new string[100];

            public httpVerb httpMethod { get; set; }

            public RESTClientGet()
            {
                httpMethod = httpVerb.GET;
            }

            public string makeRequest(string endPoint)
            {

                string strResponseValue = string.Empty;


            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endPoint);

                request.Method = httpMethod.ToString();

                HttpWebResponse response = null;

                try
                {
                    response = (HttpWebResponse)request.GetResponse();


                    //Proecess the resppnse stream... (could be JSON, XML or HTML etc..._

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                strResponseValue = reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    strResponseValue = "{\"errorMessages\":[\"" + ex.Message.ToString() + "\"],\"errors\":{}}";
                }
                finally
                {
                    if (response != null)
                    {
                        ((IDisposable)response).Dispose();
                    }
                }

                return strResponseValue;
            }
        }
        public class RESTClientPut
        {
            public enum httpVerb
            {
                GET,
                POST,
                PUT,
                DELETE
            }
            public string endPoint { get; set; }
            public httpVerb httpMethod { get; set; }
            
            public string[] endValue = new string[100];
            public string[] NameIdVar = new string[100];

            public RESTClientPut()
            {
                endPoint = "";
                httpMethod = httpVerb.PUT;
            }

            public string makeRequest(string NewVal, string NameVar)
            {
                string payLoad;
                payLoad = "";
                payLoad = "{\r\n\n    \"value\": \"" + NewVal + "\",\r\n\n    \"valueId\": null,\r\n\n    \"semanticId\": {\r\n\n      \"keys\": [\r\n\n        {\r\n\n          \"type\": \"ConceptDescription\",\r\n\n          \"local\": true,\r\n\n          \"value\": \"http://customer.com/cd//1/1/18EBD56F6B43D895\",\r\n\n          \"index\": 0,\r\n\n          \"idType\": \"IRI\"\r\n\n        }\r\n\n      ]\r\n\n    },\r\n\n    \"constraints\": [],\r\n\n    \"hasDataSpecification\": [],\r\n\n    \"idShort\": \"" + NameVar + "\",\r\n\n    \"category\": \"VARIABLE\",\r\n\n    \"modelType\": {\r\n\n      \"name\": \"Property\"\r\n\n    },\r\n\n    \"valueType\": {\r\n\n      \"dataObjectType\": {\r\n\n        \"name\": \"real\"\r\n\n      }\r\n\n    }\r\n\n}";
                string strResponseValue = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endPoint);
                request.Method = httpMethod.ToString();
                HttpWebResponse response = null;
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] data = encoder.GetBytes(payLoad); // a json object, or xml, whatever...
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                request.Expect = "application/json";
                request.GetRequestStream().Write(data, 0, data.Length);
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    //Proecess the resppnse stream... (could be JSON, XML or HTML etc..._
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                strResponseValue = reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    strResponseValue = "{\"errorMessages\":[\"" + ex.Message.ToString() + "\"],\"errors\":{}}";
                }
                finally
                {
                    if (response != null)
                    {
                        ((IDisposable)response).Dispose();
                    }
                }
                return strResponseValue;
            }

            public string makeRequest2(string Jfile)
            {
                string payLoad;
                payLoad = Jfile;
                string strResponseValue = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endPoint);
                request.Method = httpMethod.ToString();
                HttpWebResponse response = null;
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] data = encoder.GetBytes(payLoad); // a json object, or xml, whatever...
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                request.Expect = "application/json";
                request.GetRequestStream().Write(data, 0, data.Length);
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    //Proecess the resppnse stream... (could be JSON, XML or HTML etc..._
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                strResponseValue = reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    strResponseValue = "{\"errorMessages\":[\"" + ex.Message.ToString() + "\"],\"errors\":{}}";
                }
                finally
                {
                    if (response != null)
                    {
                        ((IDisposable)response).Dispose();
                    }
                }
                return strResponseValue;
            }


        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            /*
            if (checkBox1.Checked == true)
            {
                timer2.Enabled = true;
            }
            if (checkBox1.Checked == false)
            {
                timer2.Enabled = false;
            }
            */
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //button1.PerformClick();
        }
        /*
        private void button2_Click(object sender, EventArgs e)
        {
            
            RESTClientGet rClient = new RESTClientGet();
            String WValue;
            int t;
            string actualport = textBox5.Text;
            string actualShell;
            string NodeIDReceived;
            string [] NodeIdInMonitor = new string[100];
            actualShell = textBox1.Text.Substring(0, textBox1.TextLength - 9);
            if (actualport == "")
            {
                actualport = "51310";
            }
            string actualip = textBox4.Text;
            if (actualip == "")
            {
                actualip = "localhost";
            }
            if (MonitoredItemsLV.Items.Count == 0)
            {
                debugOutput("No Items to Update from AAS Sever Please check Monitor table");
                return;
            }
            t = 0;
            foreach (var pippo in questions.submodelElements)
            {
                rClient.NameIdVar[t] = pippo.idShort;
                t = t + 1;
            }
            debugOutput1("TRY to TAKE BY GET new value From AAS and Write in Device By OPCUA");
            for (int i = 0; i < MonitoredItemsLV.Items.Count; i++)
            {
                rClient.endPoint[i] = "http://" + actualip + ":" + actualport + "/aas/"+ actualShell + "/elements/"+ rClient.NameIdVar[i] + "/value";
                string strJSON = string.Empty;
                strJSON = rClient.makeRequest(rClient.endPoint[i]);
                var myJsonString = strJSON;
                var myJObject = JObject.Parse(myJsonString);
                var risultato = (myJObject.SelectToken("value").Value<string>());
                WValue = risultato;
                //debugOutput1(strJSON);
                //debugOutput1("JSON Deserialize Result");
                debugOutput1("Value From Server AAS for Var"+ i + " = " +risultato);
                try
                {
                // check if operation is currently allowed.
                //if (m_session == null || BrowseNodesTV.SelectedNode == null)
                //{
                //    return;
                //}
                // can only subscribe to local variables. 
                ReferenceDescription reference;
                reference = new ReferenceDescription();
                NodeIDReceived = "";
                NodeIDReceived = MonitoredItemsLV.Items[i].SubItems[1].Text;
                    //reference.NodeId = "ns=4;s=|var|PAC120-PXX01-3X-XX-XX.Application.GVL.QWERTZ.OperationalData.Spoolposition_act_avrg.value";
                reference.NodeId = NodeIDReceived;
                    //if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
                    //{
                //return;
                    //}
                    try
                {
                    WriteValue valueToWrite = new WriteValue();
                    valueToWrite.NodeId = (NodeId)reference.NodeId;
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = Convert.ToSingle(WValue);
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    m_session.Write(
                        null,
                        valuesToWrite,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);

                    if (StatusCode.IsBad(results[0]))
                    {
                        throw new ServiceResultException(results[0]);
                    }

                    DialogResult = DialogResult.OK;
                }
                catch (Exception exception)
                {
                    ClientUtils.HandleException("Error Writing Value", exception);
                }

                //new WriteValueDlg().ShowDialog(m_session, (NodeId)reference.NodeId, Attributes.Value);
                
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
            }

        }
        */
        private void debugOutput1(string strDebugText)
        {
            try
            {
                System.Diagnostics.Debug.Write(strDebugText + Environment.NewLine);
                //txtResponse.Text = txtResponse.Text + strDebugText + Environment.NewLine;
                //txtResponse.SelectionStart = txtResponse.TextLength;
                //txtResponse.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message, ToString() + Environment.NewLine);
            }

        }

        private void MenuBar_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void clientHeaderBranding1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            /*
            if (button3.Text == "Start AAS Server")
                {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                string actualport = textBox5.Text;
                string actualip = textBox4.Text;
                startInfo.Arguments = "--rest --no-security --data-path aasxs --host "+ actualip + " --port " + actualport+ " --name LocalAASX";
                startInfo.Verb= "runas";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = Application.StartupPath + "\\AAS_Server\\AasxServerWindows.exe";
                Process process = Process.Start(startInfo);
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                button3.Text = "Stop AAS Server";
                label1.Text = "Server AAS Started";
                return;
            }
            if (button3.Text == "Stop AAS Server")
            {
                foreach (var process1 in Process.GetProcessesByName("AasxServerWindows"))
                {
                    process1.Kill();
                    label1.Text = "Server AAS Stopped";
                    button3.Text = "Start AAS Server";
                }
            }
            */
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            foreach (var process1 in Process.GetProcessesByName("AasxServerWindows"))
            {
                process1.Kill();
            }

        }
        /*
        private void button4_Click(object sender, EventArgs e)
        {
            //ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;
            RootDirOPCUA = textBox2.Text;
            MonitoredItemsLV.Items.Clear();
            ReferenceDescription reference;
            string[] OPCUA_Var = new string[100];
            int i;
            i = 0;
            ListViewItem item;
            MonitoredItem monitoredItem;
            reference = new ReferenceDescription();
            foreach (var pippo in questions.submodelElements)
            {
                OPCUA_Var[i]= pippo.idShort;
                reference.NodeId = "ns=4;s=|var|"+ RootDirOPCUA +"."+ pippo.idShort+".value";
                item = CreateMonitoredItem((NodeId)reference.NodeId, Utils.Format("{0}", reference));
                monitoredItem = (MonitoredItem)item.Tag;
                if (ServiceResult.IsBad(monitoredItem.Status.Error))
                {
                    item.SubItems[8].Text = monitoredItem.Status.Error.StatusCode.ToString();
                }
                item.SubItems.Add(monitoredItem.DisplayName);
                item.SubItems[1].Text = monitoredItem.MonitoringMode.ToString();
                item.SubItems[2].Text = monitoredItem.SamplingInterval.ToString();
                item.SubItems[3].Text = DeadbandFilterToText(monitoredItem.Filter);
                m_subscription.ApplyChanges();
                i = i + 1;
            }
        }
        */
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            /*
            if (checkBox2.Checked == true)
            {
                timer1.Enabled = true;
            }
            if (checkBox2.Checked == false)
            {
                timer1.Enabled = false;
            }
            */
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //button2.PerformClick();
        }

        /*
        private void button5_Click(object sender, EventArgs e)
        {
            RESTClientGet rClient = new RESTClientGet();
            debugOutput1("TRY to TAKE STRUCTURE From AAS Server");
            string actualport = textBox5.Text;
            if (actualport == "")
            {
                actualport = "51310";
            }
            string actualip = textBox4.Text;
            if (actualip == "")
            {
                actualip = "localhost";
            }
            rClient.endPoint[0] = "http://" + actualip + ":" + actualport + "/aas/" + textBox1.Text;
            string jsonString = string.Empty;
            jsonString = rClient.makeRequest(rClient.endPoint[0]);
            questions = JsonConvert.DeserializeObject<Rootobject>(jsonString);
            try
            {
                if (questions.administration != null)
                {
                    foreach (var pippo in questions.submodelElements)
                    {
                        debugOutput1("Structure from AAS build  = " + pippo.idShort);
                    }

                }
                else
                {
                    debugOutput1("The AAS Server is STARTED? the Shell 1 is correct ?");
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        */
        /*
        private void button6_Click(object sender, EventArgs e)
        {
            RESTClientPut rClient = new RESTClientPut();
            debugOutput1("TRY to PUT STRUCTURE TO AAS Server");
            string actualport = textBox5.Text;
            if (actualport == "")
            {
                actualport = "51310";
            }
            string actualip = textBox4.Text;
            if (actualip == "")
            {
                actualip = "localhost";
            }
                rClient.endPoint = "http://" + actualip + ":" + actualport + "/aas/" + "/aas/DFP_GEN4_SHELL/submodels/OperationalData";
            //var questions = new Rootobject();
            questions.submodelElements[0].value = "100";
            questions.submodelElements[1].value = "200";
            questions.submodelElements[2].value = "300";
            questions.submodelElements[3].value = "400";
            questions.submodelElements[4].value = "500";
            questions.submodelElements[5].value = "600";
            questions.submodelElements[6].value = "700";
            questions.submodelElements[7].value = "800";
            string jsonString = string.Empty;
            jsonString= JsonConvert.SerializeObject(questions);
            jsonString = rClient.makeRequest2(jsonString);
            debugOutput1("Structure from Internal to AAS Server  = " + jsonString);
        }
        */
        private void ViewMI_Click(object sender, EventArgs e)
        {

        }

        private void loadOPCUAConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string FileName = Application.StartupPath + "\\OPCUA_Config.txt";
            StreamReader sw = new StreamReader(FileName);
            string dummy = sw.ReadLine();
            ConnectServerCTRL.ServerUrl = sw.ReadLine();
            //textBox4.Text = sw.ReadLine();
            //textBox5.Text = sw.ReadLine();
            sw.Close();
            return;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        private void saveOPCUAConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
            string FileName = Application.StartupPath + "\\OPCUA_Config.txt";
            StreamWriter sw = new StreamWriter(FileName);
            sw.WriteLine("OPCUA Configuration");
            sw.WriteLine(ConnectServerCTRL.ServerUrl);
            //sw.WriteLine(textBox4.Text);
            //sw.WriteLine(textBox5.Text);
            sw.Close();
            return;
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        private void HelpMI_Click(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            //timer2.Interval  = Int32.Parse(textBox7.Text) ;
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            //timer1.Interval = Int32.Parse(textBox6.Text);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //txtResponse.Text = "";
        }
        /*
        private void newShellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string actshell;
            string actdiropc;
            string actdummy;
            int index;
            try
            {
                // check if operation is currently allowed.
                if (m_session == null || BrowseNodesTV.SelectedNode == null)
                {
                    return;
                }

                // can only subscribe to local variables. 
                ReferenceDescription reference = (ReferenceDescription)BrowseNodesTV.SelectedNode.Tag;
                if (BrowseNodesTV.SelectedNode.Level == 6)
                {
                    actshell = BrowseNodesTV.SelectedNode.Tag + "/submodels/OperationalData/complete";
                    actdummy = BrowseNodesTV.SelectedNode.FullPath.Replace("\\",".");
                    actdummy = actdummy.Replace(".Resources", "");
                    actdummy = actdummy.Replace(".GlobalVars", "");
                    actdiropc = actdummy.Replace("DeviceSet.", "");
                    actdiropc = reference.NodeId.Identifier.ToString();
                    actdiropc = actdiropc.Replace("|var|", "");
                    //actdiropc = actdiropc + ".";
                    //index = actdummy.IndexOf(".");
                    //actdiropc = actdummy.Substring(index+1, (actdummy.Length-index-1));

                    textBox1.Text = actshell;
                    textBox2.Text = actdiropc;
                }
                if (BrowseNodesTV.SelectedNode.Level == 7)
                {
                    actshell = BrowseNodesTV.SelectedNode.Parent.Text + "/submodels/"+ BrowseNodesTV.SelectedNode.Tag+ "/complete";
                    actdummy = BrowseNodesTV.SelectedNode.FullPath.Replace("\\", ".");
                    actdummy = actdummy.Replace(".Resources", "");
                    actdummy = actdummy.Replace(".GlobalVars", "");
                    actdiropc = actdummy.Replace("DeviceSet.", "");
                    actdiropc = actdiropc+".";
                    actdiropc = reference.NodeId.Identifier.ToString();
                    actdiropc = actdiropc.Replace("|var|", "");
                    //actdiropc = actdiropc + ".";
                    textBox1.Text = actshell;
                    textBox2.Text = actdiropc;
                }
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }

        }
        */

        private void button8_Click_1(object sender, EventArgs e)
        {
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            NodeId NodeRead = new NodeId($"|var|CODESYS Control Win V3.Application.PLC_PRG.CW_From_OPCUA",4);
            var result = m_session.ReadValue(NodeRead);
        }

        private void infoTrasmissionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("=== OPC UA OPERATION LIMITS ===");

                NodeId[] nodeIds =
                {
                    new NodeId(11705, 0), // MaxNodesPerRead
                    new NodeId(11707, 0), // MaxNodesPerWrite
                    new NodeId(11710, 0), // MaxNodesPerBrowse
                    new NodeId(11711, 0), // MaxNodesPerRegisterNodes
                    new NodeId(11712, 0), // MaxNodesPerTranslateBrowsePathsToNodeIds
                    new NodeId(11714, 0)  // MaxMonitoredItemsPerCall
                };

                DataValueCollection values;
                DiagnosticInfoCollection diagnosticInfos;

                m_session.Read(
                    null,
                    0,
                    TimestampsToReturn.Neither,
                    new ReadValueIdCollection(
                        nodeIds.Select(nodeId => new ReadValueId
                        {
                            NodeId = nodeId,
                            AttributeId = Attributes.Value
                        }).ToList()),
                    out values,
                    out diagnosticInfos);

                for (int i = 0; i < values.Count; i++)
                {
                    Console.WriteLine(
                        $"{nodeIds[i]} -> " +
                        $"Status={values[i].StatusCode}, " +
                        $"Value={values[i].Value}");
                }

                Console.WriteLine("=== FINE ===");
            }
            catch (ServiceResultException ex)
            {
                MessageBox.Show(
                    $"StatusCode : {ex.StatusCode}\r\n" +
                    $"Message    : {ex.Message}\r\n" +
                    $"Inner      : {ex.InnerException?.Message}",
                    "OPC UA ERROR");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ERROR");
            }

        }
    }
}
