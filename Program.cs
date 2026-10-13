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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Opc.Ua;
using Opc.Ua.Configuration;

namespace Quickstarts.DataAccessClient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initialize the user interface.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationType = ApplicationType.Client;
            application.ConfigSectionName = "DataAccessClient";

            try
            {
                // process and command line arguments.
                if (application.ProcessCommandLine())
                {
                    return;
                }

                //load the application configuration.
                application.LoadApplicationConfiguration(false);

                // check the application certificate.
                //application.CheckApplicationInstanceCertificate(false, 0);

                // run the application interactively.
                Application.Run(new MainForm(application.ApplicationConfiguration));
            }
            catch (Exception e)
            {
                ExceptionDlg.Show(application.ApplicationName, e);
                return;
            }
        }
    }

    /// <summary>
    /// The <b>DataAccessClient</b> namespace contains classes which implement a UA Data Access Client.
    /// </summary>
    /// <exclude/>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    //
    public class Rootobject
    {
        public Semanticid semanticId { get; set; }
        public object[] qualifiers { get; set; }
        public object[] hasDataSpecification { get; set; }
        public Identification identification { get; set; }
        public Administration administration { get; set; }
        public string idShort { get; set; }
        public string category { get; set; }
        public Modeltype modelType { get; set; }
        public string kind { get; set; }
        public Submodelelement[] submodelElements { get; set; }
        public object descriptions { get; set; }
    }

    public class Semanticid
    {
        public Key[] Keys { get; set; }
    }

    public class Key
    {
        public string type { get; set; }
        public bool local { get; set; }
        public string value { get; set; }
        public int index { get; set; }
        public string idType { get; set; }
    }

    public class Identification
    {
        public string idType { get; set; }
        public string id { get; set; }
    }

    public class Administration
    {
        public string version { get; set; }
        public string revision { get; set; }
    }

    public class Modeltype
    {
        public string name { get; set; }
    }

    public class Submodelelement
    {
        public string value { get; set; }
        public object valueId { get; set; }
        public Semanticid1 semanticId { get; set; }
        public object[] constraints { get; set; }
        public object[] hasDataSpecification { get; set; }
        public string idShort { get; set; }
        public string category { get; set; }
        public Modeltype1 modelType { get; set; }
        public Valuetype valueType { get; set; }
        public string kind { get; set; }
        public object descriptions { get; set; }
    }

    public class Semanticid1
    {
        public Key1[] keys { get; set; }
    }

    public class Key1
    {
        public string type { get; set; }
        public bool local { get; set; }
        public string value { get; set; }
        public int index { get; set; }
        public string idType { get; set; }
    }

    public class Modeltype1
    {
        public string name { get; set; }
    }

    public class Valuetype
    {
        public Dataobjecttype dataObjectType { get; set; }
    }

    public class Dataobjecttype
    {
        public string name { get; set; }
    }


    //
    public class NamespaceDoc
    {
    }
}
