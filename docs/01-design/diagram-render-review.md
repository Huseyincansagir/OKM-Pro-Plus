# Diagram Render Review

## Reviewed files

- `o002-partial-shipment-workflow.png`
- `o003-partial-invoice-workflow.png`

## Findings

Both diagrams rendered successfully as readable PNG files with complete success, validation, partial-state, loop, and continuation branches. The O-002 diagram visibly includes invalid-quantity rejection, barcode/depot/address/stock validation, atomic shipment transaction, remaining quantity handling, optional new shipment, and reservation policy branch. The O-003 diagram visibly includes over-invoicing rejection, tax/price/customer/document validation, atomic invoice and current-account transaction, remaining-to-invoice branch, later invoicing loop, close/open remainder policy, and payment continuation.

The diagrams are intentionally vertical because they contain multiple decision loops and exception branches. Labels are readable at the generated resolution; no missing node or broken arrow was observed. The source Mermaid files remain the editable source, and the PNGs are delivery/review artefacts.
