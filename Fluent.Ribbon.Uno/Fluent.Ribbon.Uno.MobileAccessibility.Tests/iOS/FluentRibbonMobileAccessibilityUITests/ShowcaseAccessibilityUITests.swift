import XCTest

final class ShowcaseAccessibilityUITests: XCTestCase {
    private enum AccessibilityFailure: Error {
        case missingOrDuplicate(String)
    }

    @MainActor
    func testNativeShowcaseAccessibilityHierarchyAndActions() throws {
        continueAfterFailure = false

        let app = XCUIApplication()
        app.launch()

        defer {
            let hierarchy = XCTAttachment(string: app.debugDescription)
            hierarchy.name = "Showcase accessibility hierarchy"
            hierarchy.lifetime = .deleteOnSuccess
            add(hierarchy)
            app.terminate()
        }

        let ribbonQuery = exactLabel("Fluent Ribbon Showcase", in: app)
        XCTAssertTrue(
            ribbonQuery.firstMatch.waitForExistence(timeout: 90),
            "The native Showcase accessibility root never appeared."
        )

        let ribbon = try unique("Fluent Ribbon Showcase", in: app)
        let toolbars = try unique("Toolbars", in: app)
        let clipboard = try unique("Clipboard", in: app)
        let spinners = try unique("Spinners", in: app)
        let formatPainter = try unique("Format Painter", in: app)
        let bold = try unique("Bold", in: app)
        let rightMargin = try unique("Right margin", in: app)

        XCTAssertTrue(isSelected(toolbars), "Toolbars was not selected initially.")
        XCTAssertTrue(toolbars.isHittable, "Toolbars was not actionable.")
        XCTAssertTrue(clipboard.exists, "Clipboard group was absent.")
        XCTAssertTrue(spinners.exists, "Spinners group was absent.")
        XCTAssertTrue(formatPainter.isEnabled, "Format Painter was disabled.")
        XCTAssertTrue(formatPainter.isHittable, "Format Painter was not hittable.")
        XCTAssertTrue(
            [.button, .other].contains(formatPainter.elementType),
            "Format Painter exposed an unexpected element type: \(formatPainter.elementType)."
        )
        XCTAssertTrue(
            [.button, .switch, .checkBox, .other].contains(bold.elementType),
            "Bold exposed an unexpected element type: \(bold.elementType)."
        )
        XCTAssertTrue(
            bold.isSelected || !String(describing: bold.value ?? "").isEmpty,
            "Bold did not expose a toggle state."
        )
        XCTAssertFalse(rightMargin.frame.isEmpty, "Right margin exposed no accessible frame.")
        XCTAssertTrue(
            [.textField, .other, .picker, .stepper, .button].contains(rightMargin.elementType),
            "Right margin exposed an unexpected spinner/edit type: \(rightMargin.elementType)."
        )

        let disabledCuts = exactLabel("Cut", in: app)
            .allElementsBoundByIndex
            .filter { !$0.isEnabled }
        XCTAssertEqual(disabledCuts.count, 1, "Expected exactly one disabled Cut command.")

        assertAbsent(
            ["Text 1", "Text 2", "Grouped ToggleButton", "Toggle 2"],
            in: app,
            context: "hidden Tests descendants"
        )
        assertAbsent(
            ["Confidential", "Do Not Copy", "Draft", "Remove Watermark"],
            in: app,
            context: "closed popup descendants"
        )

        let boldBefore = stateDescription(bold)
        XCTAssertTrue(bold.isHittable, "Bold was not hittable.")
        bold.tap()
        waitForState("Bold toggle state to change") {
            self.stateDescription(bold) != boldBefore
        }
        XCTAssertNotEqual(stateDescription(bold), boldBefore, "Bold state did not change.")

        let galleries = try unique("Galleries", in: app)
        XCTAssertTrue(galleries.isHittable, "Galleries tab was not hittable.")
        galleries.tap()
        waitForState("Galleries tab selection and content") {
            self.isSelected(galleries)
                && !self.isSelected(toolbars)
                && self.exactLabel("Without Grouping", in: app).count == 1
        }

        XCTAssertTrue(isSelected(galleries), "Galleries did not become selected.")
        XCTAssertFalse(isSelected(toolbars), "Toolbars remained selected.")
        _ = try unique("Without Grouping", in: app)
        _ = try unique("With Grouping", in: app)
        _ = try unique("Fluent Ribbon Showcase", in: app)
        _ = try unique("Galleries", in: app)

        assertAbsent(
            ["Clipboard", "Spinners", "Format Painter", "Bold", "Right margin"],
            in: app,
            context: "hidden Toolbars descendants"
        )
        assertAbsent(
            ["Text 1", "Text 2", "Grouped ToggleButton", "Toggle 2"],
            in: app,
            context: "hidden Tests descendants after the tab change"
        )
        assertAbsent(
            ["Confidential", "Do Not Copy", "Draft"],
            in: app,
            context: "closed popup descendants after the tab change"
        )

        XCTAssertTrue(ribbon.exists, "The Ribbon root disappeared after interaction.")
    }

    @MainActor
    private func unique(
        _ label: String,
        in app: XCUIApplication,
        file: StaticString = #filePath,
        line: UInt = #line
    ) throws -> XCUIElement {
        let query = exactLabel(label, in: app)
        let count = query.count
        XCTAssertEqual(
            count,
            1,
            "Expected exactly one logical node labelled \(label), found \(count).",
            file: file,
            line: line
        )
        guard count == 1 else {
            throw AccessibilityFailure.missingOrDuplicate(label)
        }
        return query.firstMatch
    }

    @MainActor
    private func exactLabel(_ label: String, in app: XCUIApplication) -> XCUIElementQuery {
        app.descendants(matching: .any)
            .matching(NSPredicate(format: "label == %@", label))
    }

    @MainActor
    private func assertAbsent(
        _ labels: [String],
        in app: XCUIApplication,
        context: String,
        file: StaticString = #filePath,
        line: UInt = #line
    ) {
        let present = labels.filter { exactLabel($0, in: app).count != 0 }
        XCTAssertTrue(
            present.isEmpty,
            "Found \(context): \(present.joined(separator: ", ")).",
            file: file,
            line: line
        )
    }

    @MainActor
    private func isSelected(_ element: XCUIElement) -> Bool {
        if element.isSelected {
            return true
        }

        let value = String(describing: element.value ?? "")
            .trimmingCharacters(in: .whitespacesAndNewlines)
            .lowercased()
        return value == "1" || value == "selected" || value.contains("selected")
    }

    @MainActor
    private func stateDescription(_ element: XCUIElement) -> String {
        "\(element.isSelected)|\(String(describing: element.value ?? ""))"
    }

    @MainActor
    private func waitForState(
        _ description: String,
        timeout: TimeInterval = 20,
        condition: @escaping () -> Bool
    ) {
        let predicate = NSPredicate { _, _ in condition() }
        let expectation = XCTNSPredicateExpectation(predicate: predicate, object: nil)
        let result = XCTWaiter.wait(for: [expectation], timeout: timeout)
        XCTAssertEqual(result, .completed, "Timed out waiting for \(description).")
    }
}
