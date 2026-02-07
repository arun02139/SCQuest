// Cloudflare Worker: OpenAI Image Generation Proxy
//
// SETUP:
//   1. Install Wrangler CLI:  npm install -g wrangler
//   2. Login:                 wrangler login
//   3. Create project dir with this file as src/index.js and a wrangler.toml (see below)
//   4. Set your OpenAI key:   wrangler secret put OPENAI_API_KEY
//   5. Deploy:                wrangler deploy
//
// wrangler.toml:
//   name = "adventure-image-proxy"
//   main = "src/index.js"
//   compatibility_date = "2024-08-22"
//
// Your worker URL will be: https://adventure-image-proxy.<your-subdomain>.workers.dev

const CORS_HEADERS = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
  "Access-Control-Allow-Headers": "Content-Type",
};

export default {
  async fetch(request, env) {
    // Handle CORS preflight
    if (request.method === "OPTIONS") {
      return new Response(null, { headers: CORS_HEADERS });
    }

    if (request.method !== "POST") {
      return new Response("Method not allowed", { status: 405, headers: CORS_HEADERS });
    }

    try {
      const body = await request.json();
      const prompt = body.prompt;

      if (!prompt) {
        return Response.json(
          { error: "Missing 'prompt' field" },
          { status: 400, headers: CORS_HEADERS }
        );
      }

      // Call OpenAI Image API (gpt-image-1)
      const openaiResponse = await fetch("https://api.openai.com/v1/images/generations", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${env.OPENAI_API_KEY}`,
        },
        body: JSON.stringify({
          model: body.model || "gpt-image-1-mini",
          prompt: prompt,
          n: 1,
          size: body.size || "1024x1024",
          quality: body.quality || "low",
        }),
      });

      if (!openaiResponse.ok) {
        const err = await openaiResponse.text();
        return Response.json(
          { error: `OpenAI error: ${openaiResponse.status}`, details: err },
          { status: openaiResponse.status, headers: CORS_HEADERS }
        );
      }

      const result = await openaiResponse.json();

      // gpt-image-1 returns base64 in data[0].b64_json
      const b64 = result.data?.[0]?.b64_json;
      if (b64) {
        return Response.json({ b64_json: b64 }, { headers: CORS_HEADERS });
      }

      // dall-e-3 returns a URL in data[0].url (fallback)
      const url = result.data?.[0]?.url;
      if (url) {
        return Response.json({ url: url }, { headers: CORS_HEADERS });
      }

      return Response.json(
        { error: "Unexpected response format", raw: result },
        { status: 500, headers: CORS_HEADERS }
      );
    } catch (e) {
      return Response.json(
        { error: e.message },
        { status: 500, headers: CORS_HEADERS }
      );
    }
  },
};
