﻿using NuGet.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTEProject = EnvDTE.Project;
using IMSBuildNuGetProjectSystemThunk = System.Func<EnvDTE.Project, NuGet.ProjectManagement.INuGetProjectContext, NuGet.ProjectManagement.IMSBuildNuGetProjectSystem>;

namespace NuGet.PackageManagement.VisualStudio
{
    public static class MSBuildNuGetProjectSystemFactory
    {
        private static Dictionary<string, IMSBuildNuGetProjectSystemThunk> _factories = new Dictionary<string, IMSBuildNuGetProjectSystemThunk>(StringComparer.OrdinalIgnoreCase)
        {
            { NuGetVSConstants.WebApplicationProjectTypeGuid, (project, nuGetProjectContext) => new WebProjectSystem(project, nuGetProjectContext) },
            { NuGetVSConstants.WebSiteProjectTypeGuid, (project, nuGetProjectContext) => new WebSiteProjectSystem(project, nuGetProjectContext) },
            { NuGetVSConstants.FsharpProjectTypeGuid, (project, nuGetProjectContext) => new FSharpProjectSystem(project, nuGetProjectContext) },
            { NuGetVSConstants.WixProjectTypeGuid, (project, nuGetProjectContext) => new WixProjectSystem(project, nuGetProjectContext) },
            { NuGetVSConstants.JsProjectTypeGuid, (project, nuGetProjectContext) => new JsProjectSystem(project, nuGetProjectContext) },
            { NuGetVSConstants.WindowsStoreProjectTypeGuid, (project, nuGetProjectContext) => new WindowsStoreProjectSystem(project, nuGetProjectContext) },
            { NuGetVSConstants.DeploymentProjectTypeGuid, (project, nuGetProjectContext) => new VSMSBuildNuGetProjectSystem(project, nuGetProjectContext) }
        };

        public static IMSBuildNuGetProjectSystem CreateMSBuildNuGetProjectSystem(EnvDTEProject envDTEProject, INuGetProjectContext nuGetProjectContext)
        {
            if (envDTEProject == null)
            {
                throw new ArgumentNullException("project");
            }

            if (String.IsNullOrEmpty(envDTEProject.FullName))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    Strings.DTE_ProjectUnsupported, EnvDTEProjectUtility.GetName(envDTEProject)));
            }

#if VS14
            if (EnvDTEProjectUtility.SupportsINuGetProjectSystem(envDTEProject))
            {
                throw new InvalidOperationException("Does not support INuGetProjectSystem");
            }
#endif

            var guids = VsHierarchyUtility.GetProjectTypeGuids(envDTEProject);
            if (guids.Contains(NuGetVSConstants.CppProjectTypeGuid)) // Got a cpp project
            {
                if (!EnvDTEProjectUtility.IsClr(envDTEProject))
                    return new NativeProjectSystem(envDTEProject, nuGetProjectContext);
            }



            // Try to get a factory for the project type guid            
            foreach (var guid in guids)
            {
                IMSBuildNuGetProjectSystemThunk factory;
                if (_factories.TryGetValue(guid, out factory))
                {
                    return factory(envDTEProject, nuGetProjectContext);
                }
            }

            // Fall back to the default if we have no special project types
            return new VSMSBuildNuGetProjectSystem(envDTEProject, nuGetProjectContext);
        }
    }
}
