import React, { useState } from "react";
import { downloadFile } from "../Api";
import { saveBlob } from "../utilities/downloadHelper";

export default function DownloadButton({ userId }) {
  const [fileId, setFileId] = useState("");
  const [loading, setLoading] = useState(false);

  const handleDownload = async () => {
    if (!fileId) {
      alert("Enter fileId from Box first!");
      return;
    }

    setLoading(true);

    try {
      const { blob, filename } = await downloadFile(userId, fileId);
      saveBlob(blob, filename);
    } catch (err) {
      alert("Download failed: " + err.message);
    }

    setLoading(false);
  };

  return (
    <div style={{ marginTop: 20 }}>
      <input
        placeholder="Enter Box File ID"
        value={fileId}
        onChange={(e) => setFileId(e.target.value)}
      />
      <button onClick={handleDownload} disabled={loading}>
        {loading ? "Downloading..." : "Download File"}
      </button>
    </div>
  );
}
