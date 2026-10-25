// Módulo compartido de estilos y helpers para todos los documentos de
// CreditPulse (curso Análisis de Sistemas II). Genera documentos en
// tamaño Carta (US Letter) con una identidad visual consistente.
const {
  Document, Packer, Paragraph, TextRun, HeadingLevel, AlignmentType,
  Table, TableRow, TableCell, WidthType, ShadingType, BorderStyle,
  Header, Footer, PageNumber, PageBreak, ImageRun, convertInchesToTwip,
  LevelFormat, TabStopType, TabStopPosition, VerticalAlign, ExternalHyperlink,
} = require("docx");
const fs = require("fs");

const COLOR = {
  primary: "1F3A5F",     // azul oscuro (marca)
  accent: "3A5FCD",      // azul de acento (igual que los diagramas UML)
  accentLight: "EAF0FF",
  gray: "555555",
  lightGray: "F2F2F2",
  border: "CBD5E1",
  verde: "1E7B34",
  amarillo: "8A6D00",
  rojo: "B02A2A",
};

const FONT = "Calibri";
const PAGE_LETTER = { width: 12240, height: 15840 };

function titlePage({ titulo, subtitulo, curso, autor, fecha }) {
  return [
    new Paragraph({ text: "", spacing: { before: 1600 } }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      children: [new TextRun({ text: "CreditPulse", bold: true, size: 64, color: COLOR.primary, font: FONT })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 200, after: 100 },
      children: [new TextRun({ text: "Sistema de Análisis de Riesgo Crediticio", size: 26, color: COLOR.gray, font: FONT, italics: true })],
    }),
    new Paragraph({
      border: { bottom: { color: COLOR.accent, space: 8, style: BorderStyle.SINGLE, size: 12 } },
      spacing: { after: 500 },
      children: [new TextRun({ text: "" })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 900, after: 200 },
      children: [new TextRun({ text: titulo, bold: true, size: 40, color: COLOR.accent, font: FONT })],
    }),
    subtitulo ? new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { after: 700 },
      children: [new TextRun({ text: subtitulo, size: 24, color: COLOR.gray, font: FONT })],
    }) : new Paragraph({ text: "" }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 1400 },
      children: [new TextRun({ text: curso, size: 22, color: COLOR.primary, font: FONT, bold: true })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 100 },
      children: [new TextRun({ text: "Universidad Mariano Gálvez de Guatemala", size: 22, color: COLOR.gray, font: FONT })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 600 },
      children: [new TextRun({ text: autor, size: 22, color: COLOR.gray, font: FONT })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 100 },
      children: [new TextRun({ text: fecha, size: 22, color: COLOR.gray, font: FONT })],
    }),
    new Paragraph({ children: [new PageBreak()] }),
  ];
}

function h1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 400, after: 200 },
    border: { bottom: { color: COLOR.accent, space: 4, style: BorderStyle.SINGLE, size: 6 } },
    children: [new TextRun({ text, bold: true, color: COLOR.primary, font: FONT, size: 32 })],
  });
}

function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 320, after: 160 },
    children: [new TextRun({ text, bold: true, color: COLOR.accent, font: FONT, size: 26 })],
  });
}

function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 240, after: 120 },
    children: [new TextRun({ text, bold: true, color: COLOR.gray, font: FONT, size: 23 })],
  });
}

function p(text, opts = {}) {
  const runs = Array.isArray(text) ? text : [new TextRun({ text, font: FONT, size: 21 })];
  return new Paragraph({ spacing: { after: 160, line: 276 }, alignment: opts.align || AlignmentType.JUSTIFIED, children: runs, ...opts });
}

function pb(label, rest) {
  // Paragraph con una parte en negrita al inicio ("Etiqueta: resto")
  return new Paragraph({
    spacing: { after: 160, line: 276 },
    alignment: AlignmentType.JUSTIFIED,
    children: [
      new TextRun({ text: label, bold: true, font: FONT, size: 21, color: COLOR.primary }),
      new TextRun({ text: rest, font: FONT, size: 21 }),
    ],
  });
}

function bullets(items, opts = {}) {
  return items.map((item) => new Paragraph({
    numbering: { reference: "bullet-list", level: 0 },
    spacing: { after: 90 },
    children: Array.isArray(item) ? item : [new TextRun({ text: item, font: FONT, size: 21 })],
  }));
}

function numbered(items) {
  return items.map((item) => new Paragraph({
    numbering: { reference: "numbered-list", level: 0 },
    spacing: { after: 90 },
    children: Array.isArray(item) ? item : [new TextRun({ text: item, font: FONT, size: 21 })],
  }));
}

function cell(text, opts = {}) {
  const shading = opts.header
    ? { type: ShadingType.CLEAR, fill: COLOR.primary }
    : (opts.fill ? { type: ShadingType.CLEAR, fill: opts.fill } : undefined);
  const color = opts.header ? "FFFFFF" : (opts.color || "000000");
  return new TableCell({
    width: { size: opts.width || 2000, type: WidthType.DXA },
    shading,
    verticalAlign: VerticalAlign.CENTER,
    margins: { top: 80, bottom: 80, left: 100, right: 100 },
    children: [new Paragraph({
      alignment: opts.align || AlignmentType.LEFT,
      children: [new TextRun({ text: String(text), bold: !!opts.header || !!opts.bold, size: opts.size || 19, font: FONT, color })],
    })],
  });
}

function table(columnWidths, rows) {
  const totalWidth = columnWidths.reduce((a, b) => a + b, 0);
  return new Table({
    width: { size: totalWidth, type: WidthType.DXA },
    columnWidths,
    borders: {
      top: { style: BorderStyle.SINGLE, size: 4, color: COLOR.border },
      bottom: { style: BorderStyle.SINGLE, size: 4, color: COLOR.border },
      left: { style: BorderStyle.SINGLE, size: 4, color: COLOR.border },
      right: { style: BorderStyle.SINGLE, size: 4, color: COLOR.border },
      insideHorizontal: { style: BorderStyle.SINGLE, size: 4, color: COLOR.border },
      insideVertical: { style: BorderStyle.SINGLE, size: 4, color: COLOR.border },
    },
    rows: rows.map((r) => new TableRow({ children: r })),
  });
}

function semaforoTag(color) {
  const map = { VERDE: COLOR.verde, AMARILLO: COLOR.amarillo, ROJO: COLOR.rojo };
  return new TextRun({ text: color, bold: true, color: map[color] || COLOR.gray, font: FONT, size: 19 });
}

function image(path, widthPx, heightPx) {
  return new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { before: 200, after: 200 },
    children: [new ImageRun({ data: fs.readFileSync(path), transformation: { width: widthPx, height: heightPx }, type: "png" })],
  });
}

function caption(text) {
  return new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 300 },
    children: [new TextRun({ text, italics: true, size: 18, color: COLOR.gray, font: FONT })],
  });
}

function pageHeader(docTitle) {
  return new Header({
    children: [new Paragraph({
      tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }],
      border: { bottom: { color: COLOR.border, space: 4, style: BorderStyle.SINGLE, size: 4 } },
      children: [
        new TextRun({ text: "CreditPulse", bold: true, color: COLOR.primary, font: FONT, size: 16 }),
        new TextRun({ text: `\t${docTitle}`, italics: true, color: COLOR.gray, font: FONT, size: 16 }),
      ],
    })],
  });
}

function pageFooter() {
  return new Footer({
    children: [new Paragraph({
      alignment: AlignmentType.CENTER,
      children: [
        new TextRun({ text: "Página ", size: 16, color: COLOR.gray, font: FONT }),
        new TextRun({ children: [PageNumber.CURRENT], size: 16, color: COLOR.gray, font: FONT }),
        new TextRun({ text: " de ", size: 16, color: COLOR.gray, font: FONT }),
        new TextRun({ children: [PageNumber.TOTAL_PAGES], size: 16, color: COLOR.gray, font: FONT }),
      ],
    })],
  });
}

function buildAndSave(outPath, { docTitle, body }) {
  const doc = new Document({
    numbering: {
      config: [
        { reference: "bullet-list", levels: [{ level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: convertInchesToTwip(0.35), hanging: convertInchesToTwip(0.2) } } } }] },
        { reference: "numbered-list", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: convertInchesToTwip(0.35), hanging: convertInchesToTwip(0.2) } } } }] },
      ],
    },
    sections: [{
      properties: { page: { size: PAGE_LETTER, margin: { top: 1080, bottom: 1080, left: 1260, right: 1260 } } },
      headers: { default: pageHeader(docTitle) },
      footers: { default: pageFooter() },
      children: body,
    }],
  });
  return Packer.toBuffer(doc).then((buf) => fs.writeFileSync(outPath, buf));
}

module.exports = {
  COLOR, FONT, TextRun, Paragraph, AlignmentType, HeadingLevel, BorderStyle,
  titlePage, h1, h2, h3, p, pb, bullets, numbered, cell, table, image, caption,
  semaforoTag, buildAndSave, PageBreak,
};
