"use client";

import * as React from "react";
import { Radio } from "lucide-react";

import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import type { AllocatedBlockDto, RangeResponse } from "@/lib/types";

interface SpectrumVisualizerProps {
  range: RangeResponse;
  allocations: AllocatedBlockDto[];
  colorMap: Map<string, string>;
  selectedId: string | null;
  onSelect: (id: string) => void;
}

const VIEW_WIDTH = 1000;
const VIEW_HEIGHT = 260;
const PLOT_LEFT = 40;
const PLOT_RIGHT = VIEW_WIDTH - 40;
const PLOT_TOP = 16;
const PLOT_BOTTOM = 196;
const GRID_LINES = 10;

function formatMHz(mhz: number) {
  return mhz >= 1000
    ? `${(mhz / 1000).toFixed(2)} GHz`
    : `${mhz.toFixed(0)} MHz`;
}

export function SpectrumVisualizer({
  range,
  allocations,
  colorMap,
  selectedId,
  onSelect,
}: SpectrumVisualizerProps) {
  const freqRange = range.maxFreqGHz - range.minFreqGHz;
  const plotWidth = PLOT_RIGHT - PLOT_LEFT;

  const toX = React.useCallback(
    (freqGHz: number) =>
      PLOT_LEFT + ((freqGHz - range.minFreqGHz) / freqRange) * plotWidth,
    [range.minFreqGHz, freqRange, plotWidth]
  );

  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between gap-2 space-y-0">
        <CardTitle className="flex items-center gap-2">
          <Radio className="size-4 text-primary" />
          Frekans Spektrumu
        </CardTitle>
        <span className="text-sm text-muted-foreground">
          {range.minFreqGHz.toFixed(1)} – {range.maxFreqGHz.toFixed(1)} GHz
        </span>
      </CardHeader>
      <div className="px-6 pb-6">
        <svg
          viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`}
          className="w-full text-muted-foreground"
          role="img"
          aria-label="Frekans spektrumu görselleştirmesi"
        >
          {/* ızgara çizgileri + frekans etiketleri */}
          {Array.from({ length: GRID_LINES + 1 }, (_, i) => {
            const x = PLOT_LEFT + (plotWidth / GRID_LINES) * i;
            const freq = range.minFreqGHz + (freqRange / GRID_LINES) * i;
            return (
              <g key={i}>
                <line
                  x1={x}
                  y1={PLOT_TOP}
                  x2={x}
                  y2={PLOT_BOTTOM}
                  stroke="currentColor"
                  strokeOpacity={0.15}
                  strokeWidth={1}
                />
                <text
                  x={x}
                  y={PLOT_BOTTOM + 20}
                  fontSize={11}
                  textAnchor="middle"
                  fill="currentColor"
                >
                  {freq.toFixed(1)}
                </text>
              </g>
            );
          })}

          {/* çerçeve */}
          <rect
            x={PLOT_LEFT}
            y={PLOT_TOP}
            width={plotWidth}
            height={PLOT_BOTTOM - PLOT_TOP}
            fill="none"
            stroke="currentColor"
            strokeOpacity={0.3}
            strokeWidth={1.5}
            rx={6}
          />

          {allocations.length === 0 && (
            <text
              x={VIEW_WIDTH / 2}
              y={(PLOT_TOP + PLOT_BOTTOM) / 2}
              textAnchor="middle"
              fontSize={14}
              fill="currentColor"
              fillOpacity={0.6}
            >
              Henüz tahsis yok
            </text>
          )}

          {allocations.map((block) => {
            const x1 = toX(block.startFreqGHz);
            const x2 = toX(block.endFreqGHz);
            const width = Math.max(x2 - x1, 1.5);
            const centerX = (x1 + x2) / 2;
            const color = colorMap.get(block.allocationId) ?? "currentColor";
            const isSelected = block.allocationId === selectedId;

            return (
              <g
                key={block.allocationId}
                onClick={() => onSelect(block.allocationId)}
                className="cursor-pointer"
              >
                <title>
                  {`ID: ${block.allocationId}\n${block.centerFrequencyGHz.toFixed(3)} GHz merkez\n${formatMHz(block.bandwidthMHz)} bant genişliği\n[${block.startFreqGHz.toFixed(3)} – ${block.endFreqGHz.toFixed(3)}] GHz`}
                </title>
                <rect
                  x={x1}
                  y={PLOT_TOP}
                  width={width}
                  height={PLOT_BOTTOM - PLOT_TOP}
                  fill={color}
                  fillOpacity={isSelected ? 0.55 : 0.35}
                  stroke={color}
                  strokeWidth={isSelected ? 2.5 : 1.5}
                  rx={4}
                />
                <line
                  x1={centerX}
                  y1={PLOT_TOP}
                  x2={centerX}
                  y2={PLOT_BOTTOM}
                  stroke={color}
                  strokeWidth={1.5}
                  strokeDasharray="4 3"
                />
                {width > 46 && (
                  <text
                    x={centerX}
                    y={(PLOT_TOP + PLOT_BOTTOM) / 2}
                    textAnchor="middle"
                    fontSize={10.5}
                    fontWeight={600}
                    fill={color}
                  >
                    <tspan x={centerX} dy="-0.3em">
                      {block.centerFrequencyGHz.toFixed(2)} GHz
                    </tspan>
                    <tspan x={centerX} dy="1.2em">
                      {block.bandwidthMHz.toFixed(0)} MHz
                    </tspan>
                  </text>
                )}
              </g>
            );
          })}
        </svg>
      </div>
    </Card>
  );
}
