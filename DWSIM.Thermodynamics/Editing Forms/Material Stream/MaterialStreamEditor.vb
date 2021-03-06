﻿Imports DWSIM.Interfaces.Enums.GraphicObjects
Imports System.Windows.Forms
Imports Converter = DWSIM.SharedClasses.SystemsOfUnits.Converter
Imports WeifenLuo.WinFormsUI.Docking
Imports su = DWSIM.SharedClasses.SystemsOfUnits

Public Class MaterialStreamEditor

    Inherits WeifenLuo.WinFormsUI.Docking.DockContent

    Public Property MatStream As Streams.MaterialStream

    Public Loaded As Boolean = False

    Private dontshowtooltip As Boolean = False

    Dim units As SharedClasses.SystemsOfUnits.Units
    Dim nf, nff As String

    Private Sub MaterialStreamEditor_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        UpdateInfo()

    End Sub

    Sub UpdateInfo()

        units = MatStream.FlowSheet.FlowsheetOptions.SelectedUnitSystem
        nf = MatStream.FlowSheet.FlowsheetOptions.NumberFormat
        nff = MatStream.FlowSheet.FlowsheetOptions.FractionNumberFormat

        Loaded = False

        With MatStream

            'first block

            chkActive.Checked = MatStream.GraphicObject.Active

            Me.Text = .GetDisplayName() & ": " & .GraphicObject.Tag

            lblTag.Text = .GraphicObject.Tag
            If .Calculated Then
                lblStatus.Text = .FlowSheet.GetTranslatedString("Calculado") & " (" & .LastUpdated.ToString & ")"
                lblStatus.ForeColor = Drawing.Color.Blue
            Else
                If Not .GraphicObject.Active Then
                    lblStatus.Text = .FlowSheet.GetTranslatedString("Inativo")
                    lblStatus.ForeColor = Drawing.Color.Gray
                ElseIf .ErrorMessage <> "" Then
                    If .ErrorMessage.Length > 50 Then
                        lblStatus.Text = .FlowSheet.GetTranslatedString("Erro") & " (" & .ErrorMessage.Substring(50) & "...)"
                    Else
                        lblStatus.Text = .FlowSheet.GetTranslatedString("Erro") & " (" & .ErrorMessage & ")"
                    End If
                    lblStatus.ForeColor = Drawing.Color.Red
                Else
                    lblStatus.Text = .FlowSheet.GetTranslatedString("NoCalculado")
                    lblStatus.ForeColor = Drawing.Color.Black
                End If
            End If

            lblConnectedTo.Text = ""

            If .IsSpecAttached Then lblConnectedTo.Text = .FlowSheet.SimulationObjects(.AttachedSpecId).GraphicObject.Tag
            If .IsAdjustAttached Then lblConnectedTo.Text = .FlowSheet.SimulationObjects(.AttachedAdjustId).GraphicObject.Tag

            'connections

            Dim objlist As String() = .FlowSheet.SimulationObjects.Values.Where(Function(x) TypeOf x Is SharedClasses.UnitOperations.BaseClass Or x.GraphicObject.ObjectType = ObjectType.OT_Recycle).Select(Function(m) m.GraphicObject.Tag).ToArray

            cbInlet.Items.Clear()
            cbInlet.Items.AddRange(objlist)

            cbOutlet.Items.Clear()
            cbOutlet.Items.AddRange(objlist)

            If .GraphicObject.InputConnectors(0).IsAttached Then cbInlet.SelectedItem = .GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.Tag
            If .GraphicObject.OutputConnectors(0).IsAttached Then cbOutlet.SelectedItem = .GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo.Tag

            'conditions

            cbSpec.SelectedIndex = .SpecType

            cbUnitsT.Items.Clear()
            cbUnitsT.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.temperature).ToArray)
            cbUnitsT.SelectedItem = units.temperature

            cbUnitsP.Items.Clear()
            cbUnitsP.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.pressure).ToArray)
            cbUnitsP.SelectedItem = units.pressure

            cbUnitsQ.Items.Clear()
            cbUnitsQ.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.volumetricFlow).ToArray)
            cbUnitsQ.SelectedItem = units.volumetricFlow

            cbUnitsW.Items.Clear()
            cbUnitsW.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.massflow).ToArray)
            cbUnitsW.SelectedItem = units.massflow

            cbUnitsM.Items.Clear()
            cbUnitsM.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.molarflow).ToArray)
            cbUnitsM.SelectedItem = units.molarflow

            cbUnitsH.Items.Clear()
            cbUnitsH.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.enthalpy).ToArray)
            cbUnitsH.SelectedItem = units.enthalpy

            cbUnitsS.Items.Clear()
            cbUnitsS.Items.AddRange(units.GetUnitSet(Interfaces.Enums.UnitOfMeasure.entropy).ToArray)
            cbUnitsS.SelectedItem = units.entropy

            tbTemp.Text = su.Converter.ConvertFromSI(units.temperature, .Phases(0).Properties.temperature.GetValueOrDefault).ToString(nf)
            tbPressure.Text = su.Converter.ConvertFromSI(units.pressure, .Phases(0).Properties.pressure.GetValueOrDefault).ToString(nf)
            tbMassFlow.Text = su.Converter.ConvertFromSI(units.massflow, .Phases(0).Properties.massflow.GetValueOrDefault).ToString(nf)
            tbMoleFlow.Text = su.Converter.ConvertFromSI(units.molarflow, .Phases(0).Properties.molarflow.GetValueOrDefault).ToString(nf)
            tbVolFlow.Text = su.Converter.ConvertFromSI(units.volumetricFlow, .Phases(0).Properties.volumetric_flow.GetValueOrDefault).ToString(nf)
            tbEnth.Text = su.Converter.ConvertFromSI(units.enthalpy, .Phases(0).Properties.enthalpy.GetValueOrDefault).ToString(nf)
            tbEntr.Text = su.Converter.ConvertFromSI(units.entropy, .Phases(0).Properties.entropy.GetValueOrDefault).ToString(nf)

            If rbSpecVapor.Checked Then
                tbFracSpec.Text = .Phases(2).Properties.molarfraction.GetValueOrDefault.ToString(nf)
            ElseIf rbSpecLiquid.Checked Then
                tbFracSpec.Text = .Phases(1).Properties.molarfraction.GetValueOrDefault.ToString(nf)
            Else
                tbFracSpec.Text = .Phases(7).Properties.molarfraction.GetValueOrDefault.ToString(nf)
            End If

            'composition

            cbCompBasis.SelectedIndex = 0

            gridInputComposition.Rows.Clear()
            gridInputComposition.Columns(1).CellTemplate.Style.Format = nff
            For Each comp In .Phases(0).Compounds.Values
                gridInputComposition.Rows(gridInputComposition.Rows.Add(New Object() {comp.Name, comp.MoleFraction})).Cells(0).Style.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Control)
            Next

            Dim sum As Double = 0.0#
            For Each row As DataGridViewRow In gridInputComposition.Rows
                sum += Double.Parse(row.Cells(1).Value)
            Next
            lblInputAmount.Text = "Total: " & sum.ToString(nf)
            Me.lblInputAmount.ForeColor = Drawing.Color.Blue

            'property package

            Dim proppacks As String() = .FlowSheet.PropertyPackages.Values.Select(Function(m) m.Tag).ToArray
            cbPropPack.Items.Clear()
            cbPropPack.Items.AddRange(proppacks)
            cbPropPack.SelectedItem = .PropertyPackage.Tag

            Dim flashalgos As String() = .FlowSheet.FlowsheetOptions.FlashAlgorithms.Select(Function(x) x.Tag).ToArray
            cbFlashAlg.Items.Clear()
            cbFlashAlg.Items.Add("Default")
            cbFlashAlg.Items.AddRange(flashalgos)
            If .PreferredFlashAlgorithmTag <> "" Then cbFlashAlg.SelectedItem = .PreferredFlashAlgorithmTag Else cbFlashAlg.SelectedIndex = 0

            'annotation

            Try
                rtbAnnotations.Rtf = .Annotation
            Catch ex As Exception

            End Try

            cbCalculatedAmountsBasis.SelectedIndex = 0

            If .Calculated Then

                If Not TabControlMain.TabPages.Contains(TabPageResultsComp) Then TabControlMain.TabPages.Insert(1, TabPageResultsComp)
                If Not TabControlMain.TabPages.Contains(TabPageResultsProps) Then TabControlMain.TabPages.Insert(2, TabPageResultsProps)

                'result compositions

                TabPhaseComps.TabPages.Clear()
                TabPhaseComps.TabPages.Add(tabCompMix)

                PopulateCompGrid(gridCompMixture, .Phases(0).Compounds.Values.ToList, cbCalculatedAmountsBasis.SelectedItem.ToString)
                If .Phases(2).Properties.molarfraction.HasValue Then
                    PopulateCompGrid(gridCompVapor, .Phases(2).Compounds.Values.ToList, cbCalculatedAmountsBasis.SelectedItem.ToString)
                    TabPhaseComps.TabPages.Add(tabCompVapor)
                Else
                    TabPhaseComps.TabPages.Remove(tabCompVapor)
                End If
                If .Phases(3).Properties.molarfraction.GetValueOrDefault > 0.0# AndAlso .Phases(4).Properties.molarfraction.GetValueOrDefault > 0.0# Then
                    PopulateCompGrid(gridCompLiqMix, .Phases(1).Compounds.Values.ToList, cbCalculatedAmountsBasis.SelectedItem.ToString)
                    TabPhaseComps.TabPages.Add(tabCompLiqMix)
                Else
                    TabPhaseComps.TabPages.Remove(tabCompLiqMix)
                End If
                If .Phases(3).Properties.molarfraction.HasValue Then
                    PopulateCompGrid(gridCompLiq1, .Phases(3).Compounds.Values.ToList, cbCalculatedAmountsBasis.SelectedItem.ToString)
                    TabPhaseComps.TabPages.Add(tabCompLiq1)
                Else
                    TabPhaseComps.TabPages.Remove(tabCompLiq1)
                End If
                If .Phases(4).Properties.molarfraction.HasValue Then
                    PopulateCompGrid(gridCompLiq2, .Phases(4).Compounds.Values.ToList, cbCalculatedAmountsBasis.SelectedItem.ToString)
                    TabPhaseComps.TabPages.Add(tabCompLiq2)
                Else
                    TabPhaseComps.TabPages.Remove(tabCompLiq2)
                End If
                If .Phases(7).Properties.molarfraction.HasValue Then
                    PopulateCompGrid(gridCompSolid, .Phases(7).Compounds.Values.ToList, cbCalculatedAmountsBasis.SelectedItem.ToString)
                    TabPhaseComps.TabPages.Add(tabCompSolid)
                Else
                    TabPhaseComps.TabPages.Remove(tabCompSolid)
                End If

                'result properties

                TabPhaseProps.TabPages.Clear()
                TabPhaseProps.TabPages.Add(tabPropsMix)

                MatStream.PropertyPackage.CurrentMaterialStream = MatStream

                PopulatePropGrid(gridPropertiesMixture, .Phases(0))
                If .Phases(2).Properties.molarfraction.HasValue Then
                    PopulatePropGrid(gridPropertiesVapor, .Phases(2))
                    TabPhaseProps.TabPages.Add(tabPropsVapor)
                Else
                    TabPhaseProps.TabPages.Remove(tabPropsVapor)
                End If
                If .Phases(1).Properties.molarfraction.GetValueOrDefault > 0.0# Then
                    PopulatePropGrid(gridPropertiesLiqMix, .Phases(1))
                    TabPhaseProps.TabPages.Add(tabPropsLiqMix)
                Else
                    TabPhaseProps.TabPages.Remove(tabPropsLiqMix)
                End If
                If .Phases(3).Properties.molarfraction.HasValue Then
                    PopulatePropGrid(gridPropertiesLiq1, .Phases(3))
                    TabPhaseProps.TabPages.Add(tabPropsLiq1)
                Else
                    TabPhaseProps.TabPages.Remove(tabPropsLiq1)
                End If
                If .Phases(4).Properties.molarfraction.HasValue Then
                    PopulatePropGrid(gridPropertiesLiq2, .Phases(4))
                    TabPhaseProps.TabPages.Add(tabPropsLiq2)
                Else
                    TabPhaseProps.TabPages.Remove(tabPropsLiq2)
                End If
                If .Phases(7).Properties.molarfraction.HasValue Then
                    PopulatePropGrid(gridPropertiesSolid, .Phases(7))
                    TabPhaseProps.TabPages.Add(tabPropsSolid)
                Else
                    TabPhaseProps.TabPages.Remove(tabPropsSolid)
                End If

            Else

                If TabControlMain.TabPages.Contains(TabPageResultsComp) Then TabControlMain.TabPages.Remove(TabPageResultsComp)
                If TabControlMain.TabPages.Contains(TabPageResultsProps) Then TabControlMain.TabPages.Remove(TabPageResultsProps)

            End If

            If .GraphicObject.InputConnectors(0).IsAttached Then
                If .GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom.ObjectType = ObjectType.OT_Recycle Then
                    TabPageInput.Enabled = True
                Else
                    TabPageInput.Enabled = False
                End If
            Else
                TabPageInput.Enabled = True
            End If

        End With

        Loaded = True

    End Sub

    Sub PopulateCompGrid(grid As DataGridView, complist As List(Of Interfaces.ICompound), amounttype As String)

        grid.ReadOnly = True
        grid.Rows.Clear()
        grid.Columns(1).CellTemplate.Style.Format = nff
        For Each comp In complist
            grid.Rows(grid.Rows.Add(New Object() {comp.Name, comp.MoleFraction})).Cells(0).Style.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Control)
        Next

    End Sub

    Sub PopulatePropGrid(grid As DataGridView, p As Interfaces.IPhase)

        grid.ReadOnly = True
        grid.Rows.Clear()
        grid.Columns(1).CellTemplate.Style.Format = nf

        Dim refval As Nullable(Of Double), val As Double

        With grid.Rows

            refval = p.Properties.enthalpy.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.enthalpy, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("EntalpiaEspecfica"), val, units.enthalpy})
            refval = p.Properties.entropy.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.entropy, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("EntropiaEspecfica"), val, units.entropy})
            refval = p.Properties.molar_enthalpy.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.molar_enthalpy, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("MolarEnthalpy"), val, units.molar_enthalpy})
            refval = p.Properties.molar_entropy.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.molar_entropy, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("MolarEntropy"), val, units.molar_entropy})
            refval = p.Properties.molecularWeight.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.molecularWeight, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Massamolar"), val, units.molecularWeight})
            refval = p.Properties.density.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.density, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Massaespecfica"), val, units.density})
            refval = p.Properties.massflow.GetValueOrDefault / Convert.ToDouble(p.Properties.density.GetValueOrDefault)
            If refval.HasValue Then val = Converter.ConvertFromSI(units.volumetricFlow, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("VazoTP"), val, units.volumetricFlow})
            refval = p.Properties.massflow.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.massflow, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Vazomssica"), val, units.massflow})
            refval = p.Properties.molarflow.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.molarflow, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Vazomolar"), val, units.molarflow})

            If p.Name <> "Mixture" Then
                refval = p.Properties.molarfraction.GetValueOrDefault
                If refval.HasValue Then val = Format(refval, nf)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("FraomolardaPhase"), val})
                refval = p.Properties.massfraction.GetValueOrDefault
                If refval.HasValue Then val = refval
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("FraomssicadaPhase"), val})
                refval = p.Properties.compressibilityFactor.GetValueOrDefault
                If refval.HasValue Then val = refval
                .Add(New Object() {"Z", val})
            End If

            refval = p.Properties.heatCapacityCp.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.heatCapacityCp, refval)
            .Add(New Object() {"Cp", val, units.heatCapacityCp})
            refval = p.Properties.heatCapacityCp.GetValueOrDefault / p.Properties.heatCapacityCv.GetValueOrDefault
            .Add(New Object() {"Cp/Cv", refval.GetValueOrDefault})
            refval = p.Properties.thermalConductivity.GetValueOrDefault
            If refval.HasValue Then val = Converter.ConvertFromSI(units.thermalConductivity, refval)
            .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Condutividadetrmica"), val, units.thermalConductivity})

            If p.Name <> "Mixture" Then
                refval = p.Properties.kinematic_viscosity.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.cinematic_viscosity, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Viscosidadecinemtica"), val, units.cinematic_viscosity})
                refval = p.Properties.viscosity.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.viscosity, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Viscosidadedinmica"), val, units.viscosity})
            End If

            If MatStream.PropertyPackage.FlashBase.FlashSettings(Interfaces.Enums.FlashSetting.CalculateBubbleAndDewPoints) = True And p.Name = "Mixture" Then
                refval = p.Properties.bubblePressure.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.pressure, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("BubblePress"), val, units.pressure})
                refval = p.Properties.dewPressure.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.pressure, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("DewPress"), val, units.pressure})
                refval = p.Properties.bubbleTemperature.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.temperature, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("BubbleTemp"), val, units.pressure})
                refval = p.Properties.dewTemperature.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.temperature, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("DewTemp"), val, units.pressure})
            End If

            If p.Name.Contains("Overall") Then
                refval = MatStream.Phases(0).Properties.surfaceTension.GetValueOrDefault
                If refval.HasValue Then val = Converter.ConvertFromSI(units.surfaceTension, refval)
                .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Tensosuperficial"), val, units.surfaceTension})
            End If

            If p.Name = "Liquid1" Then

                If TypeOf MatStream.PropertyPackage Is PropertyPackages.SeawaterPropertyPackage Then

                    Dim water As BaseClasses.Compound = (From subst As BaseClasses.Compound In MatStream.Phases(3).Compounds.Values Select subst Where subst.ConstantProperties.CAS_Number = "7732-18-5").SingleOrDefault
                    Dim salt As BaseClasses.Compound = (From subst As BaseClasses.Compound In MatStream.Phases(3).Compounds.Values Select subst Where subst.ConstantProperties.Name = "Salt").SingleOrDefault

                    Dim salinity As Double = salt.MassFraction.GetValueOrDefault / water.MassFraction.GetValueOrDefault
                    .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("Salinity"), salinity, ""})

                End If

                If TypeOf MatStream.PropertyPackage Is PropertyPackages.SourWaterPropertyPackage Then

                    refval = MatStream.Phases(3).Properties.pH.GetValueOrDefault
                    .Add(New Object() {"pH", refval, ""})

                End If

                If MatStream.PropertyPackage.IsElectrolytePP Then

                    refval = MatStream.Phases(3).Properties.pH.GetValueOrDefault
                    .Add(New Object() {"pH", refval, ""})

                    refval = MatStream.Phases(3).Properties.osmoticCoefficient.GetValueOrDefault
                    .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("OsmoticCoefficient"), refval, ""})

                    refval = MatStream.Phases(3).Properties.freezingPoint.GetValueOrDefault
                    val = Converter.ConvertFromSI(units.temperature, refval)
                    .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("FreezingPoint"), val, units.temperature})

                    refval = MatStream.Phases(3).Properties.freezingPointDepression.GetValueOrDefault
                    val = Converter.ConvertFromSI(units.deltaT, refval)
                    .Add(New Object() {MatStream.FlowSheet.GetTranslatedString("FreezingPointDepression"), val, units.deltaT})

                End If

            End If

        End With

        For Each row As DataGridViewRow In grid.Rows
            row.Cells(0).Style.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Control)
            row.Cells(2).Style.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Control)
        Next

    End Sub

    Private Sub lblTag_TextChanged(sender As Object, e As EventArgs) Handles lblTag.TextChanged
        If Loaded Then MatStream.GraphicObject.Tag = lblTag.Text
        If Loaded Then MatStream.FlowSheet.UpdateOpenEditForms()
        Me.Text = MatStream.GetDisplayName() & ": " & MatStream.GraphicObject.Tag
        DirectCast(MatStream.FlowSheet, Interfaces.IFlowsheetGUI).UpdateInterface()
        lblTag.Focus()
        lblTag.SelectionStart = Math.Max(0, lblTag.Text.Length)
        lblTag.SelectionLength = 0
    End Sub

    Private Sub cbSpec_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbSpec.SelectedIndexChanged

        tbTemp.Enabled = False
        tbPressure.Enabled = False
        tbEnth.Enabled = False
        tbEntr.Enabled = False
        tbFracSpec.Enabled = False
        rbSpecVapor.Enabled = False
        rbSpecLiquid.Enabled = False
        rbSpecSolid.Enabled = False

        MatStream.SpecType = cbSpec.SelectedIndex

        Select Case cbSpec.SelectedIndex
            Case 0
                tbTemp.Enabled = True
                tbPressure.Enabled = True
            Case 1
                tbPressure.Enabled = True
                tbEnth.Enabled = True
            Case 2
                tbPressure.Enabled = True
                tbEntr.Enabled = True
            Case 3
                tbPressure.Enabled = True
                tbFracSpec.Enabled = True
                rbSpecVapor.Checked = True
                rbSpecVapor.Enabled = True
                rbSpecLiquid.Enabled = True
            Case 4
                tbTemp.Enabled = True
                tbFracSpec.Enabled = True
                rbSpecVapor.Checked = True
                rbSpecVapor.Enabled = True
                rbSpecLiquid.Enabled = True
            Case 5
                tbPressure.Enabled = True
                tbFracSpec.Enabled = True
                rbSpecSolid.Checked = True
                rbSpecSolid.Enabled = True
        End Select

        If Loaded Then RequestCalc()

    End Sub

    Private Sub btnNormalizeInput_Click(sender As Object, e As EventArgs) Handles btnNormalizeInput.Click
        Dim total As Double = 0.0#
        For Each row As DataGridViewRow In gridInputComposition.Rows
            total += row.Cells(1).Value
        Next
        For Each row As DataGridViewRow In gridInputComposition.Rows
            row.Cells(1).Value = row.Cells(1).Value / total
        Next
    End Sub

    Private Sub btnEqualizeInput_Click(sender As Object, e As EventArgs) Handles btnEqualizeInput.Click
        Dim total As Double = 0.0#
        For Each row As DataGridViewRow In gridInputComposition.Rows
            row.Cells(1).Value = 1.0# / gridInputComposition.Rows.Count
        Next
    End Sub

    Private Sub btnEraseInput_Click(sender As Object, e As EventArgs) Handles btnEraseInput.Click
        For Each row As DataGridViewRow In gridInputComposition.Rows
            row.Cells(1).Value = 0.0#
        Next
    End Sub

    Private Sub btnCompAcceptChanges_Click(sender As Object, e As EventArgs) Handles btnCompAcceptChanges.Click

        Dim W, Q As Double

        If Me.ValidateData() Then

            Dim mmtotal As Double = 0
            Dim mtotal As Double = 0

            Select Case cbCompBasis.SelectedIndex

                Case 0

                    btnNormalizeInput_Click(sender, e)
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        MatStream.Phases(0).Compounds(row.Cells(0).Value).MoleFraction = row.Cells(1).Value
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mtotal += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MassFraction = comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / mtotal
                    Next

                Case 1

                    btnNormalizeInput_Click(sender, e)
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        MatStream.Phases(0).Compounds(row.Cells(0).Value).MassFraction = row.Cells(1).Value
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mmtotal += comp.MassFraction.GetValueOrDefault / comp.ConstantProperties.Molar_Weight
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MoleFraction = comp.MassFraction.GetValueOrDefault / comp.ConstantProperties.Molar_Weight / mmtotal
                    Next

                Case 2

                    Dim total As Double = 0
                    For Each row As DataGridViewRow In gridInputComposition.Rows
                        total += row.Cells(1).Value
                    Next
                    Q = Converter.ConvertToSI(units.molarflow, total)
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        MatStream.Phases(0).Compounds(row.Cells(0).Value).MoleFraction = row.Cells(1).Value / total
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mtotal += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight
                    Next
                    W = 0
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MassFraction = comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / mtotal
                        W += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / 1000 * Q
                    Next
                    MatStream.Phases(0).Properties.molarflow = Q
                    MatStream.Phases(0).Properties.massflow = W

                Case 3

                    Dim total As Double = 0
                    For Each row As DataGridViewRow In gridInputComposition.Rows
                        total += row.Cells(1).Value
                    Next
                    W = Converter.ConvertToSI(units.massflow, total)
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        MatStream.Phases(0).Compounds(row.Cells(0).Value).MassFraction = row.Cells(1).Value / total
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mmtotal += comp.MassFraction.GetValueOrDefault / comp.ConstantProperties.Molar_Weight
                    Next
                    Q = 0
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MoleFraction = comp.MassFraction.GetValueOrDefault / comp.ConstantProperties.Molar_Weight / mmtotal
                        Q += comp.MassFraction.GetValueOrDefault * W / comp.ConstantProperties.Molar_Weight * 1000
                    Next
                    MatStream.Phases(0).Properties.molarflow = Q
                    MatStream.Phases(0).Properties.massflow = W

                Case 5

                    'molarity = mol solute per liter solution
                    Dim n As Integer = MatStream.Phases(0).Compounds.Count
                    Dim liqdens(n - 1), nbp(n - 1) As Double
                    Dim ipp As New Thermodynamics.PropertyPackages.RaoultPropertyPackage()
                    ipp.CurrentMaterialStream = MatStream
                    Dim i As Integer = 0
                    For Each s In MatStream.Phases(0).Compounds.Values
                        nbp(i) = s.ConstantProperties.Normal_Boiling_Point
                        If 298.15 > nbp(i) Then
                            liqdens(i) = ipp.AUX_LIQDENSi(s, nbp(i))
                        Else
                            liqdens(i) = ipp.AUX_LIQDENSi(s, 298.15)
                        End If
                        i += 1
                    Next

                    Dim total As Double = 0
                    Dim val As Double = 0
                    i = 0
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        If row.Cells(0).Value.ToString.Contains("Water") Then
                            total += row.Cells(1).Value / 1000 * liqdens(i) / MatStream.Phases(0).Compounds(row.Cells(0).Value).ConstantProperties.Molar_Weight * 1000
                        Else
                            total += row.Cells(1).Value
                        End If
                        i += 1
                    Next

                    Q = total

                    i = 0
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        If row.Cells(0).Value.ToString.Contains("Water") Then
                            MatStream.Phases(0).Compounds(row.Cells(0).Value).MoleFraction = row.Cells(1).Value / 1000 * liqdens(i) / MatStream.Phases(0).Compounds(row.Cells(0).Value).ConstantProperties.Molar_Weight * 1000 / total
                        Else
                            MatStream.Phases(0).Compounds(row.Cells(0).Value).MoleFraction = row.Cells(1).Value / total
                        End If
                        i += 1
                    Next

                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mtotal += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight
                    Next

                    W = 0
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MassFraction = comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / mtotal
                        W += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / 1000 * Q
                    Next
                    MatStream.Phases(0).Properties.molarflow = Q
                    MatStream.Phases(0).Properties.massflow = W

                    ipp = Nothing

                Case 6

                    'molarity = mol solute per kg solvent

                    Dim total As Double = 0
                    Dim val As Double = 0
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        If row.Cells(0).Value.ToString.Contains("Water") Then
                            total += row.Cells(1).Value / MatStream.Phases(0).Compounds(row.Cells(0).Value).ConstantProperties.Molar_Weight * 1000
                        Else
                            total += row.Cells(1).Value
                        End If
                    Next

                    Q = total

                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        If row.Cells(0).Value.ToString.Contains("Water") Then
                            MatStream.Phases(0).Compounds(row.Cells(0).Value).MoleFraction = row.Cells(1).Value / MatStream.Phases(0).Compounds(row.Cells(0).Value).ConstantProperties.Molar_Weight * 1000 / total
                        Else
                            MatStream.Phases(0).Compounds(row.Cells(0).Value).MoleFraction = row.Cells(1).Value / total
                        End If
                    Next

                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mtotal += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight
                    Next

                    W = 0
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MassFraction = comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / mtotal
                        W += comp.MoleFraction.GetValueOrDefault * comp.ConstantProperties.Molar_Weight / 1000 * Q
                    Next
                    MatStream.Phases(0).Properties.molarflow = Q
                    MatStream.Phases(0).Properties.massflow = W

                Case 4

                    'liquid vol. frac
                    Dim n As Integer = MatStream.Phases(0).Compounds.Count
                    Dim liqdens(n - 1), nbp(n - 1), volfrac(n - 1), totalvol As Double
                    Dim ipp As New Thermodynamics.PropertyPackages.RaoultPropertyPackage()
                    ipp.CurrentMaterialStream = MatStream
                    Dim T As Double = 273.15 + 15.56 'standard temperature
                    Dim i As Integer = 0
                    totalvol = 0.0#
                    For Each s In MatStream.Phases(0).Compounds.Values
                        nbp(i) = s.ConstantProperties.Normal_Boiling_Point
                        If T > nbp(i) Then
                            liqdens(i) = ipp.AUX_LIQDENSi(s, nbp(i))
                        Else
                            liqdens(i) = ipp.AUX_LIQDENSi(s, T)
                        End If
                        i += 1
                    Next
                    mtotal = 0.0#
                    i = 0
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        mtotal += row.Cells(1).Value * liqdens(i)
                        i += 1
                    Next
                    i = 0
                    For Each row As DataGridViewRow In Me.gridInputComposition.Rows
                        MatStream.Phases(0).Compounds(row.Cells(0).Value).MassFraction = row.Cells(1).Value * liqdens(i) / mtotal
                        i += 1
                    Next
                    mmtotal = 0.0#
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        mmtotal += comp.MassFraction.GetValueOrDefault / comp.ConstantProperties.Molar_Weight
                    Next
                    For Each comp In MatStream.Phases(0).Compounds.Values
                        comp.MoleFraction = comp.MassFraction.GetValueOrDefault / comp.ConstantProperties.Molar_Weight / mmtotal
                    Next
                    ipp = Nothing

            End Select

            RequestCalc()

        End If

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    End Sub

    Function ValidateData() As Boolean

        For Each row As DataGridViewRow In Me.gridInputComposition.Rows
            If Not Double.TryParse(row.Cells(1).Value, New Double) Then
                Return False
            End If
        Next
        Return True

    End Function

    Private Sub cbCompBasis_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbCompBasis.SelectedIndexChanged

        UpdateCompBasis(cbCompBasis, gridInputComposition, MatStream.Phases(0))

    End Sub

    Sub UpdateCompBasis(cb As ComboBox, grid As DataGridView, phase As Interfaces.IPhase)

        Dim W, Q As Double, suffix As String = ""
        W = phase.Properties.massflow.GetValueOrDefault
        Q = phase.Properties.molarflow.GetValueOrDefault
        Select Case cb.SelectedIndex
            Case 0
                For Each row As DataGridViewRow In grid.Rows
                    row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MoleFraction
                Next
            Case 1
                For Each row As DataGridViewRow In grid.Rows
                    row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MassFraction
                Next
            Case 2
                For Each row As DataGridViewRow In grid.Rows
                    row.Cells(1).Value = Converter.ConvertFromSI(units.molarflow, phase.Compounds(row.Cells(0).Value).MoleFraction.GetValueOrDefault * Q)
                Next
                suffix = units.molarflow
            Case 3
                For Each row As DataGridViewRow In grid.Rows
                    row.Cells(1).Value = Converter.ConvertFromSI(units.massflow, phase.Compounds(row.Cells(0).Value).MassFraction.GetValueOrDefault * W)
                Next
                suffix = units.massflow
            Case 5
                'molarity = mol solute per liter solution
                Dim n As Integer = phase.Compounds.Count
                Dim liqdens(n - 1), nbp(n - 1) As Double
                Dim ipp As New Thermodynamics.PropertyPackages.RaoultPropertyPackage()
                ipp.CurrentMaterialStream = MatStream
                Dim i As Integer = 0
                For Each s In phase.Compounds.Values
                    nbp(i) = s.ConstantProperties.Normal_Boiling_Point
                    If 298.15 > nbp(i) Then
                        liqdens(i) = ipp.AUX_LIQDENSi(s, nbp(i))
                    Else
                        liqdens(i) = ipp.AUX_LIQDENSi(s, 298.15)
                    End If
                    i += 1
                Next
                i = 0
                For Each row As DataGridViewRow In grid.Rows
                    If row.Cells(0).Value.ToString.Contains("Water") Then
                        row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MoleFraction.GetValueOrDefault * Q * phase.Compounds(row.Cells(0).Value).ConstantProperties.Molar_Weight / 1000 / liqdens(i) * 1000
                    Else
                        row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MoleFraction.GetValueOrDefault * Q
                    End If
                    i += 1
                Next
                suffix = "mol/L"
            Case 6
                'molarity = mol solute per kg solvent
                For Each row As DataGridViewRow In grid.Rows
                    If row.Cells(0).Value.ToString.Contains("Water") Then
                        row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MassFraction.GetValueOrDefault * W
                    Else
                        row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MoleFraction.GetValueOrDefault * Q
                    End If
                Next
                suffix = "mol/kg"
            Case 4
                'liquid vol. frac
                Dim n As Integer = phase.Compounds.Count
                Dim liqdens(n - 1), nbp(n - 1), volfrac(n - 1), totalvol As Double
                Dim ipp As New Thermodynamics.PropertyPackages.RaoultPropertyPackage()
                ipp.CurrentMaterialStream = MatStream
                Dim i As Integer = 0
                totalvol = 0.0#
                For Each s In phase.Compounds.Values
                    nbp(i) = s.ConstantProperties.Normal_Boiling_Point
                    If 298.15 > nbp(i) Then
                        liqdens(i) = ipp.AUX_LIQDENSi(s, nbp(i))
                    Else
                        liqdens(i) = ipp.AUX_LIQDENSi(s, 298.15)
                    End If
                    totalvol += s.MoleFraction.GetValueOrDefault * s.ConstantProperties.Molar_Weight / liqdens(i)
                    i += 1
                Next
                i = 0
                For Each row As DataGridViewRow In grid.Rows
                    row.Cells(1).Value = phase.Compounds(row.Cells(0).Value).MoleFraction * phase.Compounds(row.Cells(0).Value).ConstantProperties.Molar_Weight / liqdens(i) / totalvol
                    i += 1
                Next
                ipp = Nothing
        End Select

        Dim sum As Double = 0.0#
        For Each row As DataGridViewRow In grid.Rows
            sum += Double.Parse(row.Cells(1).Value)
        Next

        If cb Is cbCompBasis Then lblInputAmount.Text = "Total: " & sum.ToString(nf) & " " & suffix
        If cb Is cbCalculatedAmountsBasis Then lblAmountTotal.Text = suffix

    End Sub


    Private Sub TextBoxKeyDown(sender As Object, e As KeyEventArgs) Handles tbTemp.KeyDown, tbPressure.KeyDown, tbEnth.KeyDown, tbEntr.KeyDown,
                                                                            tbFracSpec.KeyDown, tbMassFlow.KeyDown, tbMoleFlow.KeyDown, tbVolFlow.KeyDown

        If e.KeyCode = Keys.Enter And Loaded And DirectCast(sender, TextBox).ForeColor = Drawing.Color.Blue Then

            UpdateProps(sender)

            DirectCast(sender, TextBox).SelectAll()

        End If

    End Sub

    Sub UpdateProps(sender As Object)

        Dim oldvalue, newvalue As Double, propname As String = ""

        If sender Is tbMassFlow Then
            MatStream.Phases(0).Properties.molarflow = Nothing
            MatStream.Phases(0).Properties.volumetric_flow = Nothing
        ElseIf sender Is tbMoleFlow Then
            MatStream.Phases(0).Properties.massflow = Nothing
            MatStream.Phases(0).Properties.volumetric_flow = Nothing
        ElseIf sender Is tbVolFlow Then
            MatStream.Phases(0).Properties.massflow = Nothing
            MatStream.Phases(0).Properties.molarflow = Nothing
        End If

        With MatStream.Phases(0).Properties

            If sender Is tbTemp Then
                oldvalue = .temperature.GetValueOrDefault
                .temperature = Converter.ConvertToSI(cbUnitsT.SelectedItem.ToString, Double.Parse(tbTemp.Text))
                newvalue = .temperature.GetValueOrDefault
                propname = "PROP_MS_0"
            End If
            If sender Is tbPressure Then
                oldvalue = .pressure.GetValueOrDefault
                .pressure = Converter.ConvertToSI(cbUnitsP.SelectedItem.ToString, Double.Parse(tbPressure.Text))
                newvalue = .pressure.GetValueOrDefault
                propname = "PROP_MS_1"
            End If
            If sender Is tbMassFlow Then
                oldvalue = .massflow.GetValueOrDefault
                .massflow = Converter.ConvertToSI(cbUnitsW.SelectedItem.ToString, Double.Parse(tbMassFlow.Text))
                newvalue = .massflow.GetValueOrDefault
                propname = "PROP_MS_2"
            End If
            If sender Is tbMoleFlow Then
                oldvalue = .molarflow.GetValueOrDefault
                .molarflow = Converter.ConvertToSI(cbUnitsM.SelectedItem.ToString, Double.Parse(tbMoleFlow.Text))
                newvalue = .molarflow.GetValueOrDefault
                propname = "PROP_MS_3"
            End If
            If sender Is tbVolFlow Then
                oldvalue = .volumetric_flow.GetValueOrDefault
                .volumetric_flow = Converter.ConvertToSI(cbUnitsQ.SelectedItem.ToString, Double.Parse(tbVolFlow.Text))
                newvalue = .volumetric_flow.GetValueOrDefault
                propname = "PROP_MS_4"
            End If
            If sender Is tbEnth Then
                oldvalue = .enthalpy.GetValueOrDefault
                .enthalpy = Converter.ConvertToSI(cbUnitsH.SelectedItem.ToString, Double.Parse(tbEnth.Text))
                newvalue = .enthalpy.GetValueOrDefault
                propname = "PROP_MS_7"
            End If
            If sender Is tbEntr Then
                oldvalue = .entropy.GetValueOrDefault
                .entropy = Converter.ConvertToSI(cbUnitsS.SelectedItem.ToString, Double.Parse(tbEntr.Text))
                newvalue = .entropy.GetValueOrDefault
                propname = "PROP_MS_8"
            End If

        End With

        If sender Is tbFracSpec And rbSpecVapor.Checked Then
            oldvalue = MatStream.Phases(2).Properties.molarfraction.GetValueOrDefault
            MatStream.Phases(2).Properties.molarfraction = Double.Parse(tbFracSpec.Text)
            newvalue = MatStream.Phases(2).Properties.molarfraction.GetValueOrDefault
            propname = "PROP_MS_27"
        ElseIf sender Is tbFracSpec And rbSpecLiquid.Checked Then
            oldvalue = 1.0# - MatStream.Phases(2).Properties.molarfraction.GetValueOrDefault
            MatStream.Phases(2).Properties.molarfraction = 1.0# - Double.Parse(tbFracSpec.Text)
            newvalue = 1.0# - MatStream.Phases(2).Properties.molarfraction.GetValueOrDefault
            propname = "PROP_MS_27"
        ElseIf sender Is tbFracSpec And rbSpecSolid.Checked Then
            oldvalue = MatStream.Phases(7).Properties.molarfraction.GetValueOrDefault
            MatStream.Phases(7).Properties.molarfraction = Double.Parse(tbFracSpec.Text)
            newvalue = MatStream.Phases(7).Properties.molarfraction.GetValueOrDefault
            propname = "PROP_MS_146"
        End If

        MatStream.FlowSheet.AddUndoRedoAction(New SharedClasses.UndoRedoAction() With {.AType = Interfaces.Enums.UndoRedoActionType.SimulationObjectPropertyChanged,
                                                                .ObjID = MatStream.Name,
                                                                .OldValue = oldvalue,
                                                                .NewValue = newvalue,
                                                                .PropertyName = propname,
                                                                .Tag = MatStream.FlowSheet.FlowsheetOptions.SelectedUnitSystem,
                                                                .Name = String.Format(MatStream.FlowSheet.GetTranslatedString("UndoRedo_FlowsheetObjectPropertyChanged"), MatStream.GraphicObject.Tag, MatStream.FlowSheet.GetTranslatedString(.PropertyName), .OldValue, .NewValue)})
        
        RequestCalc()

    End Sub

    Private Sub cbPropPack_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbPropPack.SelectedIndexChanged
        If Loaded Then
            MatStream.PropertyPackage = MatStream.FlowSheet.PropertyPackages.Values.Where(Function(x) x.Tag = cbPropPack.SelectedItem.ToString).SingleOrDefault
            RequestCalc()
        End If
    End Sub

    Private Sub cbFlashAlg_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbFlashAlg.SelectedIndexChanged
        If Loaded Then
            MatStream.PreferredFlashAlgorithmTag = cbFlashAlg.SelectedItem
            RequestCalc()
        End If
    End Sub

    Sub RequestCalc()

        MatStream.FlowSheet.RequestCalculation(MatStream)

    End Sub

    Private Sub tbTemp_TextChanged(sender As Object, e As EventArgs) Handles tbTemp.TextChanged, tbPressure.TextChanged, tbEnth.TextChanged, tbEntr.TextChanged,
                                                                            tbFracSpec.TextChanged, tbMassFlow.TextChanged, tbMoleFlow.TextChanged, tbVolFlow.TextChanged

        Dim tbox = DirectCast(sender, TextBox)

        If Double.TryParse(tbox.Text, New Double()) Then
            tbox.ForeColor = Drawing.Color.Blue
        Else
            tbox.ForeColor = Drawing.Color.Red
        End If

    End Sub

    Private Sub cbCalculatedAmountsBasis_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbCalculatedAmountsBasis.SelectedIndexChanged

        UpdateCompBasis(cbCalculatedAmountsBasis, gridCompMixture, MatStream.Phases(0))
        UpdateCompBasis(cbCalculatedAmountsBasis, gridCompVapor, MatStream.Phases(2))
        UpdateCompBasis(cbCalculatedAmountsBasis, gridCompLiqMix, MatStream.Phases(1))
        UpdateCompBasis(cbCalculatedAmountsBasis, gridCompLiq1, MatStream.Phases(3))
        UpdateCompBasis(cbCalculatedAmountsBasis, gridCompLiq2, MatStream.Phases(4))
        UpdateCompBasis(cbCalculatedAmountsBasis, gridCompSolid, MatStream.Phases(7))

    End Sub

    Private Sub gridInputComposition_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles gridInputComposition.CellValueChanged

        If Loaded Then
            Try
                Dim sum As Double = 0.0#
                For Each row As DataGridViewRow In gridInputComposition.Rows
                    sum += Double.Parse(row.Cells(1).Value)
                Next
                lblInputAmount.Text = "Total: " & sum.ToString(nf)
                Me.lblInputAmount.ForeColor = Drawing.Color.Blue
                If Not dontshowtooltip Then
                    ToolTip2.Show(ToolTip1.GetToolTip(btnCompAcceptChanges), btnCompAcceptChanges, 2000)
                    dontshowtooltip = True
                End If
            Catch ex As Exception
                Me.lblInputAmount.ForeColor = Drawing.Color.Red
            End Try
        End If

    End Sub

    Private Sub cbInlet_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbInlet.SelectedIndexChanged
        If Loaded Then UpdateInletConnection(sender)
    End Sub

    Private Sub cbOutlet_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbOutlet.SelectedIndexChanged
        If Loaded Then UpdateOutletConnection(sender)
    End Sub

    Private Sub btnDisconnectI_Click(sender As Object, e As EventArgs) Handles btnDisconnectI.Click

        If cbInlet.SelectedItem IsNot Nothing Then
            MatStream.FlowSheet.DisconnectObjects(MatStream.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom, MatStream.GraphicObject)
            cbInlet.SelectedItem = Nothing
        End If

    End Sub

    Private Sub btnDisconnectO_Click(sender As Object, e As EventArgs) Handles btnDisconnectO.Click

        If cbOutlet.SelectedItem IsNot Nothing Then
            MatStream.FlowSheet.DisconnectObjects(MatStream.GraphicObject, MatStream.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo)
            cbOutlet.SelectedItem = Nothing
        End If

    End Sub

    Sub UpdateInletConnection(cb As ComboBox)

        Dim text As String = cb.Text

        If text <> "" Then

            Dim gobj = MatStream.GraphicObject
            Dim flowsheet = MatStream.FlowSheet

            Dim i As Integer = 0
            For Each oc As Interfaces.IConnectionPoint In flowsheet.GetFlowsheetSimulationObject(text).GraphicObject.OutputConnectors
                If Not oc.IsAttached Then
                    If gobj.InputConnectors(0).IsAttached Then flowsheet.DisconnectObjects(gobj.InputConnectors(0).AttachedConnector.AttachedFrom, gobj)
                    flowsheet.ConnectObjects(flowsheet.GetFlowsheetSimulationObject(text).GraphicObject, gobj, i, 0)
                    Exit Sub
                End If
                i += 1
            Next

            MessageBox.Show(flowsheet.GetTranslatedString("Todasasconexespossve"), flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)

        End If

    End Sub

    Sub UpdateOutletConnection(cb As ComboBox)

        Dim text As String = cb.Text

        If text <> "" Then

            Dim gobj = MatStream.GraphicObject
            Dim flowsheet = MatStream.FlowSheet

            Dim i As Integer = 0
            For Each ic As Interfaces.IConnectionPoint In flowsheet.GetFlowsheetSimulationObject(text).GraphicObject.InputConnectors
                If Not ic.IsAttached Then
                    If gobj.OutputConnectors(0).IsAttached Then flowsheet.DisconnectObjects(gobj, gobj.OutputConnectors(0).AttachedConnector.AttachedTo)
                    flowsheet.ConnectObjects(gobj, flowsheet.GetFlowsheetSimulationObject(text).GraphicObject, 0, i)
                    Exit Sub
                End If
                i += 1
            Next

            MessageBox.Show(flowsheet.GetTranslatedString("Todasasconexespossve"), flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)

        End If

    End Sub

    Private Sub cbUnitsT_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbUnitsT.SelectedIndexChanged, cbUnitsP.SelectedIndexChanged,
                                                                                        cbUnitsW.SelectedIndexChanged, cbUnitsM.SelectedIndexChanged,
                                                                                        cbUnitsQ.SelectedIndexChanged, cbUnitsH.SelectedIndexChanged,
                                                                                        cbUnitsS.SelectedIndexChanged

        If Loaded Then
            Try
                If sender Is cbUnitsT Then
                    tbTemp.Text = Converter.Convert(cbUnitsT.SelectedItem.ToString, units.temperature, Double.Parse(tbTemp.Text)).ToString(nf)
                    cbUnitsT.SelectedItem = units.temperature
                    UpdateProps(tbTemp)
                ElseIf sender Is cbUnitsP Then
                    tbPressure.Text = Converter.Convert(cbUnitsP.SelectedItem.ToString, units.pressure, Double.Parse(tbPressure.Text)).ToString(nf)
                    cbUnitsP.SelectedItem = units.pressure
                    UpdateProps(tbPressure)
                ElseIf sender Is cbUnitsW Then
                    tbMassFlow.Text = Converter.Convert(cbUnitsW.SelectedItem.ToString, units.massflow, Double.Parse(tbMassFlow.Text)).ToString(nf)
                    cbUnitsW.SelectedItem = units.massflow
                    UpdateProps(tbMassFlow)
                ElseIf sender Is cbUnitsM Then
                    tbMoleFlow.Text = Converter.Convert(cbUnitsM.SelectedItem.ToString, units.molarflow, Double.Parse(tbMoleFlow.Text)).ToString(nf)
                    cbUnitsM.SelectedItem = units.molarflow
                    UpdateProps(tbMoleFlow)
                ElseIf sender Is cbUnitsQ Then
                    tbVolFlow.Text = Converter.Convert(cbUnitsQ.SelectedItem.ToString, units.volumetricFlow, Double.Parse(tbVolFlow.Text)).ToString(nf)
                    cbUnitsQ.SelectedItem = units.volumetricFlow
                    UpdateProps(tbVolFlow)
                ElseIf sender Is cbUnitsH Then
                    tbEnth.Text = Converter.Convert(cbUnitsH.SelectedItem.ToString, units.enthalpy, Double.Parse(tbEnth.Text)).ToString(nf)
                    cbUnitsH.SelectedItem = units.enthalpy
                    UpdateProps(tbEnth)
                ElseIf sender Is cbUnitsS Then
                    tbEntr.Text = Converter.Convert(cbUnitsS.SelectedItem.ToString, units.entropy, Double.Parse(tbEntr.Text)).ToString(nf)
                    cbUnitsS.SelectedItem = units.entropy
                    UpdateProps(tbEntr)
                End If

            Catch ex As Exception
                MatStream.FlowSheet.ShowMessage(ex.Message.ToString, Interfaces.IFlowsheet.MessageType.GeneralError)
            End Try
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles btnUtils.Click
        UtilitiesCtxMenu.Show(btnUtils, New Drawing.Point(20, 0))
    End Sub

    Private Sub DiagramaDeFasesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DiagramaDeFasesToolStripMenuItem.Click, BinaryTSMI.Click, TernaryTSMI.Click,
                                                                                                PetroleumPropsTSMI.Click, HydratesTSMI.Click, TCPTSMI.Click

        Dim utility As Interfaces.IAttachedUtility = Nothing

        If sender Is DiagramaDeFasesToolStripMenuItem Then
            utility = MatStream.FlowSheet.GetUtility(Interfaces.Enums.FlowsheetUtility.PhaseEnvelope)
            utility.Name = "PhaseEnvelope" & (MatStream.AttachedUtilities.Where(Function(x) x.GetUtilityType = Interfaces.Enums.FlowsheetUtility.PhaseEnvelope).Count + 1).ToString
        ElseIf sender Is BinaryTSMI Then
            utility = MatStream.FlowSheet.GetUtility(Interfaces.Enums.FlowsheetUtility.PhaseEnvelopeBinary)
            utility.Name = "BinaryEnvelope" & (MatStream.AttachedUtilities.Where(Function(x) x.GetUtilityType = Interfaces.Enums.FlowsheetUtility.PhaseEnvelopeBinary).Count + 1).ToString
        ElseIf sender Is TernaryTSMI Then
            utility = MatStream.FlowSheet.GetUtility(Interfaces.Enums.FlowsheetUtility.PhaseEnvelopeTernary)
            utility.Name = "TernaryEnvelope" & (MatStream.AttachedUtilities.Where(Function(x) x.GetUtilityType = Interfaces.Enums.FlowsheetUtility.PhaseEnvelopeTernary).Count + 1).ToString
        ElseIf sender Is PetroleumPropsTSMI Then
            utility = MatStream.FlowSheet.GetUtility(Interfaces.Enums.FlowsheetUtility.PetroleumProperties)
            utility.Name = "PetroleumProperties" & (MatStream.AttachedUtilities.Where(Function(x) x.GetUtilityType = Interfaces.Enums.FlowsheetUtility.PetroleumProperties).Count + 1).ToString
        ElseIf sender Is HydratesTSMI Then
            utility = MatStream.FlowSheet.GetUtility(Interfaces.Enums.FlowsheetUtility.NaturalGasHydrates)
            utility.Name = "NaturalGasHydrates" & (MatStream.AttachedUtilities.Where(Function(x) x.GetUtilityType = Interfaces.Enums.FlowsheetUtility.NaturalGasHydrates).Count + 1).ToString
        ElseIf sender Is TCPTSMI Then
            utility = MatStream.FlowSheet.GetUtility(Interfaces.Enums.FlowsheetUtility.TrueCriticalPoint)
            utility.Name = "TrueCriticalPoint" & (MatStream.AttachedUtilities.Where(Function(x) x.GetUtilityType = Interfaces.Enums.FlowsheetUtility.TrueCriticalPoint).Count + 1).ToString
        End If

        utility.AttachedTo = MatStream

        With DirectCast(utility, DockContent)
            .ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.Float
        End With

        MatStream.AttachedUtilities.Add(utility)
        MatStream.FlowSheet.DisplayForm(utility)

        AddHandler DirectCast(utility, DockContent).FormClosed, Sub()
                                                                    MatStream.AttachedUtilities.Remove(utility)
                                                                    utility.AttachedTo = Nothing
                                                                End Sub

    End Sub

    Private Sub UtilitiesCtxMenu_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles UtilitiesCtxMenu.Opening

        For Each item In MatStream.AttachedUtilities
            Dim ts As New ToolStripMenuItem(item.Name)
            AddHandler ts.Click, Sub()
                                     Dim f = DirectCast(item, DockContent)
                                     If f.Visible Then
                                         f.Select()
                                     Else
                                         MatStream.FlowSheet.DisplayForm(f)
                                     End If
                                 End Sub
            UtilitiesCtxMenu.Items.Add(ts)
            AddHandler UtilitiesCtxMenu.Closed, Sub() If UtilitiesCtxMenu.Items.Contains(ts) Then UtilitiesCtxMenu.Items.Remove(ts)
            AddHandler DirectCast(item, DockContent).FormClosed, Sub()
                                                                     MatStream.AttachedUtilities.Remove(item)
                                                                     item.AttachedTo = Nothing
                                                                 End Sub
        Next

    End Sub

    Private Sub btnConfigurePP_Click(sender As Object, e As EventArgs) Handles btnConfigurePP.Click
        MatStream.FlowSheet.PropertyPackages.Values.Where(Function(x) x.Tag = cbPropPack.SelectedItem.ToString).SingleOrDefault.DisplayEditingForm()
    End Sub

    Private Sub btnConfigureFlashAlg_Click(sender As Object, e As EventArgs) Handles btnConfigureFlashAlg.Click

        Thermodynamics.Calculator.ConfigureFlashInstance(MatStream, cbFlashAlg.SelectedItem.ToString)

    End Sub

    Private Sub rtbAnnotations_RtfChanged(sender As Object, e As EventArgs) Handles rtbAnnotations.RtfChanged
        If Loaded Then MatStream.Annotation = rtbAnnotations.Rtf
    End Sub

    Private Sub chkActive_CheckedChanged(sender As Object, e As EventArgs) Handles chkActive.CheckedChanged
        If Loaded Then MatStream.GraphicObject.Active = chkActive.Checked
    End Sub

    Private Sub gridInputComposition_KeyDown(sender As Object, e As KeyEventArgs) Handles gridInputComposition.KeyDown
        If e.KeyCode = Keys.V And e.Modifiers = Keys.Control Then PasteData(gridInputComposition)
    End Sub

    Private Sub rbSpecVapor_CheckedChanged(sender As Object, e As EventArgs) Handles rbSpecVapor.CheckedChanged, rbSpecLiquid.CheckedChanged, rbSpecSolid.CheckedChanged

        If Loaded Then
            If rbSpecVapor.Checked Then
                tbFracSpec.Text = MatStream.Phases(2).Properties.molarfraction.GetValueOrDefault.ToString(nf)
            ElseIf rbSpecLiquid.Checked Then
                tbFracSpec.Text = MatStream.Phases(1).Properties.molarfraction.GetValueOrDefault.ToString(nf)
            Else
                tbFracSpec.Text = MatStream.Phases(7).Properties.molarfraction.GetValueOrDefault.ToString(nf)
            End If
        End If

    End Sub
End Class