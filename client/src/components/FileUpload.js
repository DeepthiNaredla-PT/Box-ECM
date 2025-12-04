import React, { useState } from "react";
import { fileTypeCheck } from "../utilities/scormHelper";
import { uploadSmallFile, uploadLargeFile, uploadVersion, uploadLargeFileDirectlyToBox, uploadSCORMFile } from "../Api";

export default function FileUpload({ userId }) {
  const [file, setFile] = useState(null);
  const [result, setResult] = useState("");
  const [showConfirm, setShowConfirm] = useState(false);
  const [conflictFileId, setConflictFileId] = useState(null);

  const onUpload = async () => {
    if (!file) {
      alert("Select a file first");
      return;
    }

    let response;
    setResult("Uploading...");
    const sizeInMB = (file.size / (1024 * 1024)).toFixed(2);
    const fileType = await fileTypeCheck(file);
    console.log("File type check: ", fileType);
    if(fileType.isZip && fileType.isScorm) {
      setResult("File size: " + sizeInMB + " MB. and detected as SCORM." + "Uploading through chunk upload directly from UI to Box");
      response = await uploadSCORMFile(userId, file, () => {});
    }
    else if (file.size < 20 * 1024 * 1024) {
      setResult("File size: " + sizeInMB + " MB. " + "Uploading through simple upload");
      response = await uploadSmallFile(userId, file);
    }
    else if (file.size < 30 * 1024 * 1024) {
      setResult("File size: " + sizeInMB + " MB. " + "Uploading through chunk upload from backend API");
      response = await uploadLargeFile(userId, file);
      
    }
    else {
      setResult("File size: " + sizeInMB + " MB. " + "Uploading through chunk upload directly from UI to Box");
      response = await uploadLargeFileDirectlyToBox(userId, file, () => {});
    }

    if (response.conflict) {
      // 409 error → Ask user if they want to upload new version
      setConflictFileId(response.fileId);
      setShowConfirm(true);
      setResult(`File exists: ${response.fileName}. Upload new version?`);
      return;
    }

    setResult(JSON.stringify(response, null, 2));
  };

  const confirmUploadVersion = async () => {
    setShowConfirm(false);
    const response = await uploadVersion(userId, conflictFileId, file);

    setResult("Uploaded new file version:\n" + JSON.stringify(response, null, 2));
  };

  return (
    <div>

      <input type="file" onChange={(e) => setFile(e.target.files[0])} />

      <br /><br />

      <button onClick={onUpload}>Upload</button>

      {showConfirm && (
        <div style={{ marginTop: 20, padding: 10, border: "1px solid red" }}>
          <p>This file already exists. Upload a new version?</p>
          <button onClick={confirmUploadVersion}>Yes, upload version</button>
          <button onClick={() => setShowConfirm(false)}>Cancel</button>
        </div>
      )}

      <pre style={{ marginTop: 20 }}>{result}</pre>

    </div>
  );
}
