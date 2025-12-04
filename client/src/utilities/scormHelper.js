import JSZip from "jszip";

export async function fileTypeCheck(file) {

  // 1️⃣ EXTENSION CHECK
  if (!validateZipExtension(file)) {
    console.log("Not a zip based on extension");
    return { isZip: false, isScorm: false };
  }

  // 3️⃣ LOAD ZIP AND LOOK FOR MANIFEST
  const zip = await JSZip.loadAsync(file);
  const isScorm = Object.keys(zip.files).some(f =>
    f.toLowerCase().split("/").pop() === "imsmanifest.xml"
  );
  console.log("SCORM manifest found: ", isScorm);

  return { isZip: true, isScorm };
}

// Helper:
function validateZipExtension(file) {
  var check = file.name.toLowerCase().endsWith(".zip");
  console.log("Zip extension check: ", check);
  return check;
}
