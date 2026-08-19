import { Plugin } from 'chart.js';

/**
 * Draws external "callout" labels for pie/doughnut charts: a short leader line runs from each
 * slice's outer edge out to a label showing the category name and its value/percentage, instead of
 * cramming text inside slices (which is unreadable on small slices, and hides under the doughnut hole).
 * Labels that would land too close together on the same side are nudged apart vertically so they
 * stay legible. This runs as part of the chart's normal draw, so it's also captured when the chart
 * is exported as an image (e.g. into the management report PDF).
 */
export const outsidePieLabelsPlugin: Plugin<'pie' | 'doughnut'> = {
  id: 'outsidePieLabels',
  afterDraw(chart) {
    const type = (chart.config as { type: string }).type;
    if (type !== 'pie' && type !== 'doughnut') return;

    const meta = chart.getDatasetMeta(0);
    const dataset = chart.data.datasets[0];
    if (!meta?.data?.length || !dataset) return;

    const values = (dataset.data as number[]).map(Number);
    const total = values.reduce((a, b) => a + b, 0);
    if (!total) return;

    const colors = (dataset.backgroundColor as string[]) || [];
    const labels = (chart.data.labels as string[]) || [];
    const { ctx, chartArea } = chart;

    const LEADER = 14; // radial segment length beyond the slice edge
    const ELBOW = 26;  // horizontal segment length
    const GAP = 30;    // minimum vertical gap between stacked labels on the same side

    interface Item { i: number; angle: number; cx: number; cy: number; r: number; side: 1 | -1; anchorY: number; }
    const left: Item[] = [];
    const right: Item[] = [];

    meta.data.forEach((arc: any, i: number) => {
      if (!values[i]) return;
      const angle = (arc.startAngle + arc.endAngle) / 2;
      const cos = Math.cos(angle), sin = Math.sin(angle);
      const side: 1 | -1 = cos >= 0 ? 1 : -1;
      const anchorY = arc.y + (arc.outerRadius + LEADER) * sin;
      (side === 1 ? right : left).push({ i, angle, cx: arc.x, cy: arc.y, r: arc.outerRadius, side, anchorY });
    });

    // Sort top-to-bottom, then push overlapping labels apart; re-clamp inside the chart area.
    const declutter = (items: Item[]) => {
      if (!items.length) return;
      items.sort((a, b) => a.anchorY - b.anchorY);
      for (let k = 1; k < items.length; k++) {
        const min = items[k - 1].anchorY + GAP;
        if (items[k].anchorY < min) items[k].anchorY = min;
      }
      const bottomOverflow = items[items.length - 1].anchorY - (chartArea.bottom - 4);
      if (bottomOverflow > 0) items.forEach(it => (it.anchorY -= bottomOverflow));
      const topOverflow = (chartArea.top + 4) - items[0].anchorY;
      if (topOverflow > 0) items.forEach(it => (it.anchorY += topOverflow));
    };
    declutter(left);
    declutter(right);

    ctx.save();
    ctx.lineWidth = 1.5;
    ctx.textBaseline = 'middle';

    [...left, ...right].forEach(({ i, angle, cx, cy, r, side, anchorY }) => {
      const color = colors[i] || '#6b7280';
      const startX = cx + r * Math.cos(angle);
      const startY = cy + r * Math.sin(angle);
      const bendX = cx + side * (r + LEADER);
      const bendY = anchorY;
      const endX = bendX + side * ELBOW;

      ctx.strokeStyle = color;
      ctx.beginPath();
      ctx.moveTo(startX, startY);
      ctx.lineTo(bendX, bendY);
      ctx.lineTo(endX, bendY);
      ctx.stroke();

      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.arc(startX, startY, 2.5, 0, Math.PI * 2);
      ctx.fill();

      const pct = Math.round((values[i] / total) * 1000) / 10;
      const textX = endX + side * 4;
      ctx.textAlign = side === 1 ? 'left' : 'right';

      ctx.font = 'bold 10.5px Helvetica, Arial, sans-serif';
      ctx.fillStyle = '#374151';
      ctx.fillText(String(labels[i] ?? ''), textX, bendY - 6);

      ctx.font = '10px Helvetica, Arial, sans-serif';
      ctx.fillStyle = '#6b7280';
      ctx.fillText(`${values[i]} (${pct}%)`, textX, bendY + 6);
    });

    ctx.restore();
  }
};
