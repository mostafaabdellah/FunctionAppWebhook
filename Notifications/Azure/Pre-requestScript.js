// Set Date header value for authorization
// Should be UTC GMT string
pm.environment.set("header_date", new Date().toUTCString());

// Get hash of all header-name:value
const headers = pm.request.getHeaders({ ignoreCase: true, enabled: true });

// Construct Signature value for Authorization header
var signatureParts = [
    pm.request.method.toUpperCase(),
    headers["content-encoding"] || "",
    headers["content-language"] || "",
    headers["content-length"] || "",
    //    pm.request.body ? pm.request.body.toString().length || "" : "",
    headers["content-md5"] || "",
    headers["content-type"] || "",
    headers["x-ms-date"] ? "" : (pm.environment.get("header_date") || ""),
    headers["if-modified-since"] || "",
    headers["if-match"] || "",
    headers["if-none-match"] || "",
    headers["if-unmodified-since"] || "",
    headers["range"] || ""
];

// Construct CanonicalizedHeaders
const canonicalHeaderNames = [];
Object.keys(headers).forEach(key => {
    if (key.startsWith("x-ms-")) {
        canonicalHeaderNames.push(key);
    }
});
// Sort headers lexographically by name
canonicalHeaderNames.sort();

const canonicalHeaderParts = [];
canonicalHeaderNames.forEach(key => {
    let value = pm.request.getHeaders({ ignoreCase: true, enabled: true })[key];

    // Populate environment
    value = pm.environment.replaceIn(value);

    // Replace whitespace in value but not if its within quotes
    if (!value.startsWith("\"")) {
        value = value.replace(/\s+/, " ");
    }

    canonicalHeaderParts.push(`${key}:${value}`);
});

// Add headers to signature
signatureParts.push.apply(signatureParts, canonicalHeaderParts);

// Construct CanonicalizedResource
const canonicalResourceParts = [
    `/${pm.environment.get("azureStorageAccount")}${pm.request.url.getPath()}`
];
const canonicalQueryNames = [];
pm.request.url.query.each(query => {
    canonicalQueryNames.push(query.key.toLowerCase());
});
canonicalQueryNames.sort();
canonicalQueryNames.forEach(queryName => {
    const value = pm.request.url.query.get(queryName);

    // NOTE: This does not properly explode multiple same query params' values
    // and turn them into comma-separated list
    canonicalResourceParts.push(`${queryName}:${value}`);
});
// Add resource to signature
signatureParts.push.apply(signatureParts, canonicalResourceParts);

console.log("Signature Parts", signatureParts);

// Now, construct signature raw string
const signatureRaw = signatureParts.join("\n");

console.log("Signature String", JSON.stringify(signatureRaw));

// Hash it using HMAC-SHA256 and then encode using base64
const storageKey = pm.environment.get("azureQueueAccessKey");
const signatureBytes = CryptoJS.HmacSHA256(signatureRaw, CryptoJS.enc.Base64.parse(storageKey));
const signatureEncoded = signatureBytes.toString(CryptoJS.enc.Base64);

console.log("Storage Account", pm.environment.get("azureStorageAccount"));
console.log("Storage Key", storageKey);

// Finally, make it available for headers
pm.environment.set("header_authorization",
    `SharedKey ${pm.environment.get("azureStorageAccount")}:${signatureEncoded}`);