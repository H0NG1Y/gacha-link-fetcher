import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

const repository = process.env.GITHUB_REPOSITORY || "H0NG1Y/gacha-link-fetcher";
const token = process.env.GITHUB_TOKEN;
const outputPath = process.argv[2] || "assets/star-history.svg";

if (!token) {
  throw new Error("GITHUB_TOKEN is required to read the repository's stargazer history.");
}

const headers = {
  Accept: "application/vnd.github.star+json",
  Authorization: `Bearer ${token}`,
  "User-Agent": "gacha-link-fetcher-star-history",
  "X-GitHub-Api-Version": "2022-11-28",
};

async function github(pathname) {
  const response = await fetch(`https://api.github.com${pathname}`, { headers });
  if (!response.ok) {
    const message = await response.text();
    throw new Error(`GitHub API ${response.status}: ${message.slice(0, 300)}`);
  }
  return response.json();
}

function day(value) {
  return new Date(value).toISOString().slice(0, 10);
}

function xml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&apos;");
}

function niceScale(maximum) {
  if (maximum <= 5) {
    return { maximum: Math.max(1, maximum), step: 1 };
  }

  const roughStep = maximum / 5;
  const magnitude = 10 ** Math.floor(Math.log10(roughStep));
  const normalized = roughStep / magnitude;
  const multiplier = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
  const step = multiplier * magnitude;
  return { maximum: Math.ceil(maximum / step) * step, step };
}

const metadata = await github(`/repos/${repository}`);
const stargazers = [];

for (let page = 1; ; page += 1) {
  const batch = await github(`/repos/${repository}/stargazers?per_page=100&page=${page}`);
  stargazers.push(...batch.filter((item) => item.starred_at));
  if (batch.length < 100) break;
}

stargazers.sort((left, right) => left.starred_at.localeCompare(right.starred_at));

const dailyTotals = new Map();
for (const stargazer of stargazers) {
  const date = day(stargazer.starred_at);
  dailyTotals.set(date, (dailyTotals.get(date) || 0) + 1);
}

let cumulative = 0;
const history = [...dailyTotals.entries()].map(([date, count]) => {
  cumulative += count;
  return { date, count: cumulative };
});

const width = 800;
const height = 500;
const margin = { top: 86, right: 34, bottom: 62, left: 70 };
const chartWidth = width - margin.left - margin.right;
const chartHeight = height - margin.top - margin.bottom;
const startDate = new Date(day(metadata.created_at));
let endDate = history.length ? new Date(history.at(-1).date) : new Date(startDate);
if (endDate <= startDate) endDate = new Date(startDate.getTime() + 86_400_000);
const duration = endDate.getTime() - startDate.getTime();
const yScale = niceScale(history.length ? history.at(-1).count : 0);

const x = (date) => margin.left + ((new Date(date).getTime() - startDate.getTime()) / duration) * chartWidth;
const y = (count) => margin.top + chartHeight - (count / yScale.maximum) * chartHeight;

const total = history.length ? history.at(-1).count : 0;
const points = [{ date: day(startDate), count: 0 }, ...history];
if (points.at(-1).date !== day(endDate)) {
  points.push({ date: day(endDate), count: total });
}

const linePoints = points.map((point) => `${x(point.date).toFixed(2)},${y(point.count).toFixed(2)}`).join(" ");
const areaPath = [
  `M ${x(points[0].date).toFixed(2)} ${y(0).toFixed(2)}`,
  ...points.map((point) => `L ${x(point.date).toFixed(2)} ${y(point.count).toFixed(2)}`),
  `L ${x(points.at(-1).date).toFixed(2)} ${y(0).toFixed(2)}`,
  "Z",
].join(" ");

const yTicks = [];
for (let value = 0; value <= yScale.maximum; value += yScale.step) {
  yTicks.push(`
    <line class="grid" x1="${margin.left}" y1="${y(value)}" x2="${width - margin.right}" y2="${y(value)}" />
    <text class="axis-label" x="${margin.left - 14}" y="${y(value) + 5}" text-anchor="end">${value}</text>`);
}

const xTicks = [];
const usedDates = new Set();
for (let index = 0; index <= 4; index += 1) {
  const tickDate = new Date(startDate.getTime() + (duration * index) / 4);
  const label = day(tickDate);
  if (usedDates.has(label)) continue;
  usedDates.add(label);
  const tickX = margin.left + (chartWidth * index) / 4;
  xTicks.push(`
    <line class="tick" x1="${tickX}" y1="${margin.top + chartHeight}" x2="${tickX}" y2="${margin.top + chartHeight + 7}" />
    <text class="axis-label" x="${tickX}" y="${margin.top + chartHeight + 27}" text-anchor="middle">${label}</text>`);
}

const circles = history.length <= 30
  ? history.map((point) => `<circle class="point" cx="${x(point.date)}" cy="${y(point.count)}" r="3.5"><title>${xml(point.date)}: ${point.count} stars</title></circle>`).join("\n    ")
  : "";

const lastUpdate = history.length ? history.at(-1).date : day(metadata.created_at);
const starLabel = total === 1 ? "star" : "stars";
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" role="img" aria-labelledby="title description">
  <title id="title">${xml(repository)} Stars History</title>
  <desc id="description">${total} current ${starLabel}. Generated automatically from GitHub stargazer data.</desc>
  <style>
    .background { fill: #ffffff; }
    .title { fill: #1f2328; font: 600 24px -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    .subtitle, .axis-label { fill: #59636e; font: 13px -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    .grid { stroke: #d8dee4; stroke-width: 1; }
    .tick { stroke: #8c959f; stroke-width: 1; }
    .area { fill: #0969da; fill-opacity: .12; }
    .line { fill: none; stroke: #0969da; stroke-width: 3; stroke-linejoin: round; stroke-linecap: round; }
    .point { fill: #ffffff; stroke: #0969da; stroke-width: 2; }
    @media (prefers-color-scheme: dark) {
      .background { fill: #0d1117; }
      .title { fill: #f0f6fc; }
      .subtitle, .axis-label { fill: #8b949e; }
      .grid { stroke: #30363d; }
      .tick { stroke: #6e7681; }
      .area { fill: #58a6ff; fill-opacity: .15; }
      .line { stroke: #58a6ff; }
      .point { fill: #0d1117; stroke: #58a6ff; }
    }
  </style>
  <rect class="background" width="${width}" height="${height}" rx="12" />
  <text class="title" x="${margin.left}" y="38">${xml(repository)} Stars History</text>
  <text class="subtitle" x="${margin.left}" y="64">${total} current ${starLabel} · Latest star data: ${lastUpdate}</text>
${yTicks.join("")}
${xTicks.join("")}
  <path class="area" d="${areaPath}" />
  <polyline class="line" points="${linePoints}" />
${circles}
</svg>
`;

await mkdir(path.dirname(outputPath), { recursive: true });
await writeFile(outputPath, svg, "utf8");
console.log(`Wrote ${outputPath} with ${total} stars.`);
