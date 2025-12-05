import { sha1DigestBase64, sha1DigestBase64ForEntireFile } from "./utilities/uploadHelper";
const API_BASE = "http://localhost:5000";
//const API_BASE = "https://localhost:7104";
//const API_BASE = "https://boxecm.local/api";
const CALL_BACK_URL = "http://localhost:3000/auth/callback";
//const CALL_BACK_URL = "https://boxecm.local/auth/callback";

export const loginToBox = () => {
  window.location.href = `${API_BASE}/boxauth/login?returnUrl=${CALL_BACK_URL}`;
};

export const getToken = async (userId) =>{
    const res = await fetch(
    `${API_BASE}/box/token?userId=${userId}`,
    {
      method: "GET"
    });
    const json = await res.json();
    return json.accessToken;
};

export const uploadSmallFile = async (userId, file) => {
  const formData = new FormData();
  formData.append("file", file);
  const res = await fetch(
    `${API_BASE}/box/upload?userId=${userId}`,
    {
      method: "POST",
      body: formData,
    }
  );
  const json = await res.json();
  // Detect Box 409 conflict
  if (json.status === 409 && json.code === "item_name_in_use") {
    return {
      conflict: true,
      fileId: json.context_Info?.conflicts?.id,
      fileName: json.context_Info?.conflicts?.name,
    };
  }
  return json;
};

export const uploadLargeFile = async (userId, file) => {
  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch(
    `${API_BASE}/box/upload-large?userId=${userId}`,
    {
      method: "POST",
      body: formData,
    }
  );
  const json = await res.json();
  // Detect Box 409 conflict
  if (res.status === 409 && json.code === "item_name_in_use") {
    return {
      conflict: true,
      fileId: json.context_info?.conflicts?.id,
      fileName: json.context_info?.conflicts?.name,
    };
  }
  return json;
};

export const uploadSCORMFile = async (userId, file, onProgress) => {
    if(file.size < 20 * 1024 * 1024)
    {
        var uploadResponse = await uploadSmallFile(userId, file);
    }
    else
    {
        var uploadResponse = await uploadLargeFileDirectlyToBox(userId, file, onProgress);
    }
    console.log("Upload response: ", uploadResponse);
    const res = await fetch(
    `${API_BASE}/box/scorm-process/${uploadResponse.id}?userId=${userId}`,
    {
      method: "GET"
    });
  console.log("SCORM process response: ", res);
  return uploadResponse
};

export const uploadLargeFileDirectlyToBox = async (userId, file, onProgress) => {
    // 1. Ask backend for upload session
    const session = await createUploadSession(userId, file.name, file.size);
    const accessToken = await getToken(userId);
    // 2. Upload chunks directly to Box
    const parts = await uploadLargeFileToBox(accessToken, session, file, onProgress);    
    // 3. Commit upload
    const commitRes = await commitUpload(userId, session.id, parts, file);
    return commitRes;
};

export const uploadVersion = async (userId, fileId, file) => {
  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch(
    `${API_BASE}/box/upload-version/${fileId}?userId=${userId}`,
    {
      method: "POST",
      body: formData,
    }
  );

  return res.json();
};

export const createUploadSession = async (userId, fileName, fileSize) => {
    const res = await fetch(
    `${API_BASE}/box/create-upload-session?userId=${userId}&fileName=${fileName}&fileSize=${fileSize}`,
    {
      method: "GET"
    });

  const response = await res.json();
  return response
};

export async function uploadLargeFileToBox(accessToken, session, file, onProgress) {
    console.log(session);
  const partSize = session.part_Size; // usually 8MB
  const sessionId = session.id;

  const totalSize = file.size;
  let uploaded = 0;

  let parts = [];

  while (uploaded < totalSize) {
    const chunk = file.slice(uploaded, uploaded + partSize);
    const chunkArrayBuffer = await chunk.arrayBuffer();
    const chunkUint8 = new Uint8Array(chunkArrayBuffer);

    const start = uploaded;
    const end = Math.min(uploaded + partSize, totalSize) - 1;

    // Box requires SHA1 digest per chunk
    const sha1 = await sha1DigestBase64(chunkUint8);

    const res = await fetch(
      `https://upload.box.com/api/2.0/files/upload_sessions/${sessionId}`,
      {
        method: "PUT",
        headers: {
          "Authorization": `Bearer ${accessToken}`,
          "Content-Range": `bytes ${start}-${end}/${totalSize}`,
          "Digest": `sha=${sha1}`
        },
        body: chunkUint8
      }
    );

    const data = await res.json();
    console.log("Parts uploaded: ", data);
    parts.push(data.part);

    uploaded += partSize;
    onProgress?.((uploaded / totalSize) * 100);
  }

  return parts;
};

export async function commitUpload(userId, sessionId, parts, file) {
  const digest = await sha1DigestBase64ForEntireFile(file);
  const res = await fetch(
    `${API_BASE}/box/commit-upload?userId=${userId}&sessionId=${sessionId}`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        parts: parts.map(p => ({
          part_id: p.part_id,
          offset: p.offset,
          size: p.size
        })),
        digest
      })
    }
  );

  return res.json();
};

export const downloadFile = async (userId, fileId) => {  
  const url = `${API_BASE}/box/download/${fileId}?userId=${userId}`;

  const response = await fetch(url, {
    method: "GET",
  });

  if (!response.ok) {
    throw new Error("Download failed");
  }

  // Convert to Blob
  const blob = await response.blob();
  // extract filename from headers
  const disposition = response.headers.get("content-disposition");
  let filename = fileId;

  if (disposition && disposition.includes("filename=")) {
    filename = disposition
      .split("filename=")[1]
      .replace(/"/g, "")
      .trim();
  }

  return { blob, filename };
};
