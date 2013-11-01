﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;

namespace IISUtil
{
    //IIS 6 did not yet support the new ServerManager component that is available in newer version
    //Instead we need to make use of the WMI (Windows Management Instrumentation) to make modifications
    //to the IIS configuration.
    public class IISWMIHelper
    {
        public static bool TryGetSiteID(String Comment, ref String SiteId)
        {
            DirectoryEntry iis = new DirectoryEntry("IIS://localhost/W3SVC");
            foreach (DirectoryEntry entry in iis.Children)
            {
                if (entry.SchemaClassName.Equals("iiswebserver", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (entry.Properties["ServerComment"].Value.ToString().Equals(Comment, StringComparison.CurrentCultureIgnoreCase))
                    {
                        SiteId = entry.Name;
                        return true;
                    }

                }
            }
            return false;
        }

    }


    //The following is examples of what the metabase looks like
    /* 
            <IIsWebServer	Location ="/LM/W3SVC/1962875481"
                    AuthFlags="0"
                    LogPluginClsid="{FF160663-DE82-11CF-BC0A-00AA006111E0}"
                    SSLCertHash="40da7e6686d698efae7172220f4da438025dfeea"
                    SSLStoreName="MY"
                    SecureBindings=":443:mshsca13.cordonco.com"
                    ServerAutoStart="TRUE"
                    ServerBindings=":80:mshsca13.cordonco.com"
                    ServerComment="MSHSCA13"
                >
            </IIsWebServer>
            <IIsFilters	Location ="/LM/W3SVC/1962875481/filters"
                    AdminACL="NOLONGERVALID02eeb9eb376fc8ffad41b6b1ffca589e7fecc3e2649b5ed4b37a42904899c0b776"
                >
            </IIsFilters>
            <IIsWebVirtualDir	Location ="/LM/W3SVC/1962875481/root"
                    AccessFlags="AccessExecute | AccessRead | AccessScript"
                    AppFriendlyName="Default Application"
                    AppIsolated="2"
                    AppRoot="/LM/W3SVC/1962875481/Root"
                    AuthFlags="AuthAnonymous | AuthNTLM"
                    DefaultDoc="index.aspx"
                    DirBrowseFlags="DirBrowseShowDate | DirBrowseShowTime | DirBrowseShowSize | DirBrowseShowExtension | DirBrowseShowLongDate | EnableDefaultDoc"
                    Path="C:\inetpub\MSHSCA13"
                    ScriptMaps=@".asp,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE
                        .cer,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE
                        .cdx,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE
                        ........ bla bla bla ................
                        .refresh,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG"
                    UNCPassword="NOLONGERVALID2ba612e2a71081820aaa907830586d4986babeddced57560e36e6e5e0a6b2f"
                >
            </IIsWebVirtualDir>
            <IIsWebDirectory	Location ="/LM/W3SVC/1962875481/root/aspnet_client"
                    AccessFlags="AccessRead"
                    DirBrowseFlags="0"
                >
            */

    public static class AccessFlags
    {
        //http://msdn.microsoft.com/en-us/library/ms525016(v=vs.90).aspx
        public const int AccessExecute = 0x00000004;
        public const int AccessRead = 0x00000001;
        public const int AccessScript = 0x00000200;
    }

    public static class AuthFlags
    {
        //http://msdn.microsoft.com/en-us/library/ms524513(v=vs.90).aspx
        //AuthFlags
        public const int AuthNTLM = 0x00000004;
        //Specifies Anonymous authentication as one of the possible Windows authentication schemes returned to clients as being available.
        public const int AuthAnonymous = 0x00000001;
    }

    public static class AspDotNetVersion
    {
        public const string AspNetV1 = "1.0.3705";
        public const string AspNetV11 = "1.1.4322";
        public const string AspNetV2 = "2.0.50727";
        public const string AspNetV4 = "4.0.30319";
    }

    public static class ScriptMapper
    {
        public static void SetASPNetVersion(DirectoryEntry siteDE)
        {
            const string targetAspNetVersion = AspDotNetVersion.AspNetV4;

            //Need to initialize the script maps for the first time if not setup yet
            if (siteDE.Properties["ScriptMaps"].Count == 0)
            {
                foreach (String sc in ScriptMaps) siteDE.Properties["ScriptMaps"].Add(sc);

            }

            //loop through the script maps
            for (int i = 0; i < siteDE.Properties["ScriptMaps"].Count; i++)
            {
                //replace the versions if they exists
                siteDE.Properties["ScriptMaps"][i] = siteDE.Properties["ScriptMaps"][i].ToString().Replace(AspDotNetVersion.AspNetV1, targetAspNetVersion);
                siteDE.Properties["ScriptMaps"][i] = siteDE.Properties["ScriptMaps"][i].ToString().Replace(AspDotNetVersion.AspNetV11, targetAspNetVersion);
                siteDE.Properties["ScriptMaps"][i] = siteDE.Properties["ScriptMaps"][i].ToString().Replace(AspDotNetVersion.AspNetV2, targetAspNetVersion);
                siteDE.Properties["ScriptMaps"][i] = siteDE.Properties["ScriptMaps"][i].ToString().Replace(AspDotNetVersion.AspNetV4, targetAspNetVersion);
            }

            siteDE.CommitChanges();
        }

        private static String[] ScriptMaps = new String[]
        {
            @".asp,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE",
            @".cer,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE",
            @".cdx,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE",
            @".asa,C:\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE",
            @".idc,C:\WINDOWS\system32\inetsrv\httpodbc.dll,5,GET,POST",
            @".shtm,C:\WINDOWS\system32\inetsrv\ssinc.dll,5,GET,POST",
            @".shtml,C:\WINDOWS\system32\inetsrv\ssinc.dll,5,GET,POST",
            @".stm,C:\WINDOWS\system32\inetsrv\ssinc.dll,5,GET,POST",
            @".asax,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".ascx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".ashx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".asmx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".aspx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".axd,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".vsdisco,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".rem,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".soap,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".config,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".cs,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".csproj,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".vb,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".vbproj,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".webinfo,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".licx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".resx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".resources,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".master,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".skin,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".compiled,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".browser,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".mdb,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".jsl,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".vjsproj,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".sitemap,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".msgx,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".ad,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".dd,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".ldd,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".sd,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".cd,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".adprototype,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".lddprototype,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".sdm,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".sdmDocument,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".ldb,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".svc,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG",
            @".mdf,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".ldf,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".java,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".exclude,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG",
            @".refresh,c:\windows\microsoft.net\framework\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG"
        };

    }
}
