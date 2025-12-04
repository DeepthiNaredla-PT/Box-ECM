import React from "react";
import FileUpload from "../components/FileUpload";
import DownloadButton from "../components/DownloadButton";

export default function UploadPage() {
  const userId = localStorage.getItem("box_user_id");
  if (!userId) {
    return (
      <div>
        <h2>No user logged in</h2>
        <a href="/">Go to Login</a>
      </div>
    );
  }

  return (
    <div style={{ padding: 40 }}>
      <h2>Upload Files to Box</h2>
      <FileUpload userId={userId} />

      <h2>Download Box File</h2>
      <DownloadButton userId={userId} />
    </div>
  );
}
