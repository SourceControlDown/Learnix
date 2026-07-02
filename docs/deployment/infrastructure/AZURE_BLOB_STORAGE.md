# Azure Blob Storage

1. Search for **Storage accounts** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Storage account name:** `learnixstorage`.
4. **Region:** `West Europe`.
5. **Performance:** Standard. **Redundancy:** LRS.
6. Click **Review + create**, then **Create**.
7. Once created, click **Containers** under Data storage.
8. Create the following containers:
   - `temp-uploads` (Set public access level to **Private**)
   - `avatars` (Set public access level to **Blob**)
   - `course-covers` (Set public access level to **Blob**)
   - `course-videos` (Set public access level to **Blob**)
   - `certificates` (Set public access level to **Private**)
9. Go to **Access keys** on the left menu and copy your **Connection string**.

### Step 6.1 — Set Up Lifecycle Policy for Temporary Uploads

To ensure unconfirmed uploads (e.g., interrupted file transfers) do not consume storage indefinitely, configure an automatic cleanup policy:

1. In your Storage Account menu, scroll down to **Data management** and select **Lifecycle management**.
2. Click **Add a rule**.
3. **Rule name:** `CleanupTempUploads`.
4. **Rule scope:** Select **Limit blobs with filters**.
5. **Blob type:** Select **Block blobs**.
6. **Blob subtype:** Select **Base blobs**.
7. Click **Next** to go to the **Base blobs** tab.
8. Set the condition: **If base blobs were Last modified more than 1 days ago**, then **Delete the blob**.
9. Click **Next** to go to the **Filter set** tab.
10. Under **Blob prefix**, type: `temp-uploads/`
11. Click **Add** to save the rule. This will automatically delete any orphaned blobs older than 24 hours in the temporary container.
