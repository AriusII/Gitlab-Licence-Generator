using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitlabLicenseGenerator.Core.Licensing;

/// <summary>
///     Hand-written converter for GitLab's license JSON shape, because the schema has cross-field
///     conditional rules plain JsonPropertyName / JsonIgnoreCondition attributes cannot express —
///     most notably that `last_synced_at` is emitted only when `next_sync_at` is present, NOT
///     when `last_synced_at` itself is present.
/// </summary>
public sealed class LicenseJsonConverter : JsonConverter<License>
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";

    public override void Write(Utf8JsonWriter writer, License value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("version", value.Version);

        writer.WritePropertyName("licensee");
        JsonSerializer.Serialize(writer, value.Licensee, options);

        // `issued_at` is the legacy JSON name GitLab uses for starts_at.
        if (value.StartsAt is { } startsAt)
            writer.WriteString("issued_at", startsAt.ToString(DateFormat, CultureInfo.InvariantCulture));

        if (value.WillExpire)
            writer.WriteString("expires_at", value.ExpiresAt!.Value.ToString(DateFormat, CultureInfo.InvariantCulture));

        if (value.WillNotifyAdmins)
            writer.WriteString("notify_admins_at",
                value.NotifyAdminsAt!.Value.ToString(DateFormat, CultureInfo.InvariantCulture));

        if (value.WillNotifyUsers)
            writer.WriteString("notify_users_at",
                value.NotifyUsersAt!.Value.ToString(DateFormat, CultureInfo.InvariantCulture));

        if (value.WillBlockChanges)
            writer.WriteString("block_changes_at",
                value.BlockChangesAt!.Value.ToString(DateFormat, CultureInfo.InvariantCulture));

        // Both fields below are gated on WillSync (next_sync_at presence) — not on last_synced_at's own
        // presence. This is an exact, easy-to-regress quirk of GitLab's own license JSON serialization.
        if (value.WillSync)
        {
            writer.WriteString("next_sync_at",
                value.NextSyncAt!.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
            if (value.LastSyncedAt is { } lastSyncedAt)
                writer.WriteString("last_synced_at",
                    lastSyncedAt.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
        }

        if (value.Activated)
            writer.WriteString("activated_at",
                value.ActivatedAt!.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture));

        writer.WriteBoolean("cloud_licensing_enabled", value.CloudLicensingEnabled);
        writer.WriteBoolean("offline_cloud_licensing_enabled", value.OfflineCloudLicensingEnabled);
        writer.WriteBoolean("auto_renew_enabled", value.AutoRenewEnabled);
        writer.WriteBoolean("seat_reconciliation_enabled", value.SeatReconciliationEnabled);
        writer.WriteBoolean("operational_metrics_enabled", value.OperationalMetricsEnabled);
        writer.WriteBoolean("contract_overages_allowed", value.ContractOveragesAllowed);
        writer.WriteBoolean("generated_from_customers_dot", value.GeneratedFromCustomersDot);
        writer.WriteBoolean("generated_from_cancellation", value.GeneratedFromCancellation);
        writer.WriteBoolean("temporary_extension", value.TemporaryExtension);

        if (value.Restricted)
        {
            writer.WritePropertyName("restrictions");
            WriteRestrictions(writer, value.Restrictions!);
        }

        writer.WriteEndObject();
    }

    private static void WriteRestrictions(Utf8JsonWriter writer, LicenseRestrictions restrictions)
    {
        writer.WriteStartObject();
        writer.WriteString("plan", restrictions.Plan);
        writer.WriteNumber("active_user_count", restrictions.ActiveUserCount);
        writer.WriteBoolean("trial", restrictions.Trial);
        writer.WriteBoolean("reconciliation_completed", restrictions.ReconciliationCompleted);

        if (restrictions.AddOns.Count > 0)
        {
            writer.WritePropertyName("add_ons");
            writer.WriteStartObject();
            foreach (var (name, count) in restrictions.AddOns) writer.WriteNumber(name, count);

            writer.WriteEndObject();
        }

        if (restrictions.AddOnProducts.Count > 0)
        {
            writer.WritePropertyName("add_on_products");
            writer.WriteStartObject();
            foreach (var (name, purchases) in restrictions.AddOnProducts)
            {
                writer.WritePropertyName(name);
                writer.WriteStartArray();
                foreach (var purchase in purchases)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("quantity", purchase.Quantity);
                    writer.WriteString("started_on",
                        purchase.StartedOn.ToString(DateFormat, CultureInfo.InvariantCulture));
                    writer.WriteString("expires_on",
                        purchase.ExpiresOn.ToString(DateFormat, CultureInfo.InvariantCulture));
                    if (purchase.PurchaseXid is { } purchaseXid) writer.WriteString("purchase_xid", purchaseXid);

                    writer.WriteBoolean("trial", purchase.Trial);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        foreach (var (feature, count) in restrictions.FeatureUserCounts) writer.WriteNumber(feature, count);

        writer.WriteEndObject();
    }

    public override License Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.TryGetProperty("version", out var versionElement)
            && versionElement.ValueKind == JsonValueKind.Number
            && versionElement.GetInt32() != 1)
            throw new JsonException(
                $"Unsupported license version '{versionElement.GetInt32()}' — only version 1 is supported.");

        var license = new License();

        if (root.TryGetProperty("licensee", out var licenseeElement) &&
            licenseeElement.ValueKind == JsonValueKind.Object)
            foreach (var property in licenseeElement.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.String)
                    license.Licensee[property.Name] = property.Value.GetString()!;

        license.StartsAt = ReadDate(root, "issued_at");
        license.ExpiresAt = ReadDate(root, "expires_at");
        license.NotifyAdminsAt = ReadDate(root, "notify_admins_at");
        license.NotifyUsersAt = ReadDate(root, "notify_users_at");
        license.BlockChangesAt = ReadDate(root, "block_changes_at");

        license.NextSyncAt = ReadDateTime(root, "next_sync_at");
        license.LastSyncedAt = ReadDateTime(root, "last_synced_at");
        license.ActivatedAt = ReadDateTime(root, "activated_at");

        license.CloudLicensingEnabled = ReadBool(root, "cloud_licensing_enabled");
        license.OfflineCloudLicensingEnabled = ReadBool(root, "offline_cloud_licensing_enabled");
        license.AutoRenewEnabled = ReadBool(root, "auto_renew_enabled");
        license.SeatReconciliationEnabled = ReadBool(root, "seat_reconciliation_enabled");
        license.OperationalMetricsEnabled = ReadBool(root, "operational_metrics_enabled");
        license.GeneratedFromCustomersDot = ReadBool(root, "generated_from_customers_dot");
        license.GeneratedFromCancellation = ReadBool(root, "generated_from_cancellation");
        license.TemporaryExtension = ReadBool(root, "temporary_extension");

        // Defaults true unless the license JSON explicitly sets it false.
        license.ContractOveragesAllowed = !(root.TryGetProperty("contract_overages_allowed", out var coa)
                                            && coa.ValueKind == JsonValueKind.False);

        if (root.TryGetProperty("restrictions", out var restrictionsElement) &&
            restrictionsElement.ValueKind == JsonValueKind.Object)
            license.Restrictions = ReadRestrictions(restrictionsElement);

        return license;
    }

    private static LicenseRestrictions ReadRestrictions(JsonElement element)
    {
        var plan = element.TryGetProperty("plan", out var planElement) && planElement.ValueKind == JsonValueKind.String
            ? planElement.GetString()!
            : string.Empty;

        var activeUserCount = element.TryGetProperty("active_user_count", out var countElement)
                              && countElement.ValueKind == JsonValueKind.Number
            ? countElement.GetInt32()
            : 0;

        var restrictions = new LicenseRestrictions
        {
            Plan = plan, ActiveUserCount = activeUserCount,
            Trial = ReadBool(element, "trial")
        };

        if (element.TryGetProperty("reconciliation_completed", out var reconciliationElement)
            && reconciliationElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            restrictions.ReconciliationCompleted = reconciliationElement.ValueKind == JsonValueKind.True;

        if (element.TryGetProperty("add_ons", out var addOnsElement) && addOnsElement.ValueKind == JsonValueKind.Object)
            foreach (var addOn in addOnsElement.EnumerateObject())
                if (addOn.Value.ValueKind == JsonValueKind.Number)
                    restrictions.AddOns[addOn.Name] = addOn.Value.GetInt32();

        if (element.TryGetProperty("add_on_products", out var addOnProductsElement) &&
            addOnProductsElement.ValueKind == JsonValueKind.Object)
            foreach (var addOnProduct in addOnProductsElement.EnumerateObject())
                restrictions.AddOnProducts[addOnProduct.Name] = ReadAddOnPurchases(addOnProduct.Value);

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("plan") || property.NameEquals("active_user_count")
                                            || property.NameEquals("trial") ||
                                            property.NameEquals("reconciliation_completed")
                                            || property.NameEquals("add_ons") || property.NameEquals("add_on_products"))
                continue;

            if (property.Value.ValueKind == JsonValueKind.Number)
                restrictions.FeatureUserCounts[property.Name] = property.Value.GetInt32();
        }

        return restrictions;
    }

    private static List<LicenseAddOnPurchase> ReadAddOnPurchases(JsonElement arrayElement)
    {
        var purchases = new List<LicenseAddOnPurchase>();
        if (arrayElement.ValueKind != JsonValueKind.Array) return purchases;

        foreach (var entry in arrayElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object) continue;

            var quantity = entry.TryGetProperty("quantity", out var quantityElement) &&
                           quantityElement.ValueKind == JsonValueKind.Number
                ? quantityElement.GetInt32()
                : 0;

            var purchaseXid = entry.TryGetProperty("purchase_xid", out var purchaseXidElement) &&
                              purchaseXidElement.ValueKind == JsonValueKind.String
                ? purchaseXidElement.GetString()
                : null;

            purchases.Add(new LicenseAddOnPurchase
            {
                Quantity = quantity,
                StartedOn = ReadDate(entry, "started_on") ?? default,
                ExpiresOn = ReadDate(entry, "expires_on") ?? default,
                PurchaseXid = purchaseXid,
                Trial = ReadBool(entry, "trial")
            });
        }

        return purchases;
    }

    private static bool ReadBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.True;
    }

    // A malformed date string is silently dropped rather than raising an error, matching GitLab's
    // own lenient date parsing.
    private static DateOnly? ReadDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) ||
            element.ValueKind != JsonValueKind.String) return null;

        return DateOnly.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? ReadDateTime(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) ||
            element.ValueKind != JsonValueKind.String) return null;

        return DateTimeOffset.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }
}