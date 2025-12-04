export async function sha1DigestBase64(byteArray) {
  const hash = await crypto.subtle.digest("SHA-1", byteArray);
  return btoa(String.fromCharCode(...new Uint8Array(hash)));
}

export async function sha1DigestBase64ForEntireFile(file) {
  const fileBuffer = await file.arrayBuffer();   // read entire file
  const hashBuffer = await crypto.subtle.digest("SHA-1", fileBuffer);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  const hashString = String.fromCharCode.apply(null, hashArray);
  return btoa(hashString);   // base64
}