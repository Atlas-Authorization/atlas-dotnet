using System;
using System.Collections.Generic;
using Atlas.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Authorization helpers that express Atlas conditions as ASP.NET Core
    /// policies. They let an app say <c>RequireAtlasPermission("billing:write")</c>
    /// in the same place it writes any other policy, and have it decided by the
    /// shared Atlas authz engine rather than a bespoke claim check.
    /// </summary>
    public static class AtlasAuthorizationExtensions
    {
        /// <summary>
        /// Require that the caller holds a specific organization permission
        /// (checked against the <c>org_permissions</c> claim).
        /// </summary>
        public static AuthorizationPolicyBuilder RequireAtlasPermission(
            this AuthorizationPolicyBuilder builder, string permission)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrEmpty(permission)) throw new ArgumentException("permission is required.", nameof(permission));
            return builder.RequireAtlas(new ProtectCondition { Permission = permission });
        }

        /// <summary>Require a specific organization role (<c>org_role</c>).</summary>
        public static AuthorizationPolicyBuilder RequireAtlasRole(
            this AuthorizationPolicyBuilder builder, string role)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrEmpty(role)) throw new ArgumentException("role is required.", nameof(role));
            return builder.RequireAtlas(new ProtectCondition { Role = role });
        }

        /// <summary>Require AT LEAST ONE of the given permissions.</summary>
        public static AuthorizationPolicyBuilder RequireAtlasAnyPermission(
            this AuthorizationPolicyBuilder builder, params string[] permissions)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.RequireAtlas(new ProtectCondition { AnyPermission = permissions });
        }

        /// <summary>Require ALL of the given permissions.</summary>
        public static AuthorizationPolicyBuilder RequireAtlasAllPermissions(
            this AuthorizationPolicyBuilder builder, params string[] permissions)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.RequireAtlas(new ProtectCondition { AllPermissions = permissions });
        }

        /// <summary>
        /// The general form: attach an arbitrary <see cref="ProtectCondition"/> to a
        /// policy. The convenience overloads above all route through here so there is
        /// a single requirement type for the authz handler to evaluate.
        /// </summary>
        public static AuthorizationPolicyBuilder RequireAtlas(
            this AuthorizationPolicyBuilder builder, ProtectCondition condition)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            // Restricting to the Atlas scheme means the permission claims the
            // requirement reads are guaranteed to be the ones THIS handler emitted,
            // not whatever another scheme happened to call "org_permissions".
            builder.AddAuthenticationSchemes(AtlasDefaults.AuthenticationScheme);
            builder.RequireAuthenticatedUser();
            builder.AddRequirements(new AtlasPermissionRequirement(condition));
            return builder;
        }

        /// <summary>
        /// Register a set of named Atlas policies in one call, e.g.
        /// <c>AddAtlasAuthorization(p =&gt; p.Add("billing", c =&gt; c.Permission = "billing:write"))</c>.
        /// A thin sugar over <c>AddAuthorization</c> for apps that prefer declaring
        /// their Atlas policies together.
        /// </summary>
        public static IServiceCollection AddAtlasAuthorization(
            this IServiceCollection services,
            Action<AtlasPolicyRegistry> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var registry = new AtlasPolicyRegistry();
            configure(registry);

            return services.AddAuthorizationBuilder()
                .AddPoliciesFrom(registry)
                .Services;
        }

        private static AuthorizationBuilder AddPoliciesFrom(
            this AuthorizationBuilder builder, AtlasPolicyRegistry registry)
        {
            foreach (var entry in registry.Policies)
            {
                var condition = entry.Value;
                builder.AddPolicy(entry.Key, p => p.RequireAtlas(condition));
            }
            return builder;
        }
    }

    /// <summary>
    /// A small collector for named Atlas policies, used by
    /// <see cref="AtlasAuthorizationExtensions.AddAtlasAuthorization"/>.
    /// </summary>
    public sealed class AtlasPolicyRegistry
    {
        internal Dictionary<string, ProtectCondition> Policies { get; } = new Dictionary<string, ProtectCondition>();

        /// <summary>Add a named policy from a condition builder.</summary>
        public AtlasPolicyRegistry Add(string name, Action<ProtectCondition> configure)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name is required.", nameof(name));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var condition = new ProtectCondition();
            configure(condition);
            Policies[name] = condition;
            return this;
        }

        /// <summary>Add a named policy requiring a single permission.</summary>
        public AtlasPolicyRegistry AddPermission(string name, string permission)
            => Add(name, c => c.Permission = permission);
    }
}
