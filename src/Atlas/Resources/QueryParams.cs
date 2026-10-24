using System.Collections.Generic;

namespace Atlas.Resources
{
    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListPlatformAppsParams
    {
        public bool? IncludeArchived { get; set; }
        public bool? Archived { get; set; }
        public string? Tag { get; set; }
        /// <summary>Keep only apps whose provisioning metadata matches every pair (<c>metadata[key]=value</c>).</summary>
        public Dictionary<string, object?>? Metadata { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("include_archived", IncludeArchived);
            yield return new KeyValuePair<string, object?>("archived", Archived);
            yield return new KeyValuePair<string, object?>("tag", Tag);
            foreach (var kv in AtlasProvisioning.MetadataQuery(Metadata)) yield return kv;
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListEventsParams
    {
        public string? Type { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        /// <summary>Bounds the feed by time (ISO 8601); wins over <see cref="From"/>.</summary>
        public string? Since { get; set; }
        /// <summary>Bounds the feed by time (ISO 8601); wins over <see cref="To"/>.</summary>
        public string? Until { get; set; }
        /// <summary><c>asc</c> or <c>desc</c>.</summary>
        public string? Order { get; set; }
        public long? Limit { get; set; }
        public string? StartingAfter { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("type", Type);
            yield return new KeyValuePair<string, object?>("from", From);
            yield return new KeyValuePair<string, object?>("to", To);
            yield return new KeyValuePair<string, object?>("since", Since);
            yield return new KeyValuePair<string, object?>("until", Until);
            yield return new KeyValuePair<string, object?>("order", Order);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class InspectRequestParams
    {
        public string? Origin { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? Token { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("origin", Origin);
            yield return new KeyValuePair<string, object?>("method", Method);
            yield return new KeyValuePair<string, object?>("path", Path);
            yield return new KeyValuePair<string, object?>("token", Token);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListConfigChangelogParams
    {
        public string? Action { get; set; }
        public string? Since { get; set; }
        /// <summary>false leaves out writes that changed nothing; null keeps the server default.</summary>
        public bool? IncludeNoOp { get; set; }
        public long? Limit { get; set; }
        public string? StartingAfter { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("action", Action);
            yield return new KeyValuePair<string, object?>("since", Since);
            yield return new KeyValuePair<string, object?>("include_no_op", IncludeNoOp);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListMachinesParams
    {
        public string? Status { get; set; }
        public string? Hostname { get; set; }
        public string? OwnerUserId { get; set; }
        public string? OrganizationId { get; set; }
        public string? Metadata { get; set; }
        public long? Limit { get; set; }
        public string? StartingAfter { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("status", Status);
            yield return new KeyValuePair<string, object?>("hostname", Hostname);
            yield return new KeyValuePair<string, object?>("owner_user_id", OwnerUserId);
            yield return new KeyValuePair<string, object?>("organization_id", OrganizationId);
            yield return new KeyValuePair<string, object?>("metadata", Metadata);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListMachineEnrolmentTokensParams
    {
        public string? OrganizationId { get; set; }
        public long? Limit { get; set; }
        public string? StartingAfter { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("organization_id", OrganizationId);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class DirectoryParams
    {
        public string? Q { get; set; }
        public string? Category { get; set; }
        public bool? Verified { get; set; }
        public long? Limit { get; set; }
        public long? Offset { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("q", Q);
            yield return new KeyValuePair<string, object?>("category", Category);
            yield return new KeyValuePair<string, object?>("verified", Verified);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("offset", Offset);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListAuditEventsParams
    {
        public string? OrganizationId { get; set; }
        public string? ActorId { get; set; }
        public string? Action { get; set; }
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
        public bool? AnyOf { get; set; }
        public string? MetadataKey { get; set; }
        public string? MetadataValue { get; set; }
        public string? StartingAfter { get; set; }
        public long? Limit { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("organization_id", OrganizationId);
            yield return new KeyValuePair<string, object?>("actor_id", ActorId);
            yield return new KeyValuePair<string, object?>("action", Action);
            yield return new KeyValuePair<string, object?>("target_type", TargetType);
            yield return new KeyValuePair<string, object?>("target_id", TargetId);
            yield return new KeyValuePair<string, object?>("from", From);
            yield return new KeyValuePair<string, object?>("to", To);
            yield return new KeyValuePair<string, object?>("any_of", AnyOf);
            yield return new KeyValuePair<string, object?>("metadata_key", MetadataKey);
            yield return new KeyValuePair<string, object?>("metadata_value", MetadataValue);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
            yield return new KeyValuePair<string, object?>("limit", Limit);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ExportAuditEventsParams
    {
        public string? OrganizationId { get; set; }
        public string? ActorId { get; set; }
        public string? Action { get; set; }
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
        public bool? AnyOf { get; set; }
        public string? MetadataKey { get; set; }
        public string? MetadataValue { get; set; }
        public string? StartingAfter { get; set; }
        public string? Format { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("organization_id", OrganizationId);
            yield return new KeyValuePair<string, object?>("actor_id", ActorId);
            yield return new KeyValuePair<string, object?>("action", Action);
            yield return new KeyValuePair<string, object?>("target_type", TargetType);
            yield return new KeyValuePair<string, object?>("target_id", TargetId);
            yield return new KeyValuePair<string, object?>("from", From);
            yield return new KeyValuePair<string, object?>("to", To);
            yield return new KeyValuePair<string, object?>("any_of", AnyOf);
            yield return new KeyValuePair<string, object?>("metadata_key", MetadataKey);
            yield return new KeyValuePair<string, object?>("metadata_value", MetadataValue);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
            yield return new KeyValuePair<string, object?>("format", Format);
        }
    }

    /// <summary>Query parameters; unset values are omitted.</summary>
    public sealed class ListEndUsersOrganizationsParams
    {
        public string? Status { get; set; }
        public long? Limit { get; set; }
        public string? StartingAfter { get; set; }

        /// <summary>Flatten to query key/value pairs, dropping unset fields.</summary>
        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("status", Status);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }
}
