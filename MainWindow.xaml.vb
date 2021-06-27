Imports Microsoft.Kinect
Imports System.IO
Imports System.Globalization
Imports System.IO.Ports

Namespace Microsoft.Samples.Kinect.SkeletonBasics
    Partial Public Class MainWindow
        Inherits Window

        Private sensor As KinectSensor
        Private colorBitmap As WriteableBitmap
        Private colorPixels() As Byte
        Private Port As SerialPort
        Dim BgrPixel As Integer = CInt(PixelFormats.Bgr32.BitsPerPixel / 8) '4


        ' ウインドウクラスの初期化
        Public Sub New()
            InitializeComponent()
        End Sub

        ' プログラム開始処理
        Private Sub WindowLoaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
            ' キネクト取得
            For Each sensorItem In KinectSensor.KinectSensors
                If sensorItem.Status = KinectStatus.Connected Then
                    Me.sensor = sensorItem
                    Exit For
                End If
            Next sensorItem

            ' シリアルポート関連
            Port = New SerialPort("COM3", 9600, Parity.None, 8, StopBits.One)
            Port.Open()
            Port.WriteLine("start")
            Me.statusBarText.Text = "Start"


            ' キネクト見つかっってたらイベントとコードの紐付け
            If Nothing IsNot Me.sensor Then
                ' 各センサの有効化
                Me.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30)
                Me.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30)
                Me.sensor.SkeletonStream.Enable()

                ' フレーム取得ルーチンへのハンドラ
                AddHandler Me.sensor.AllFramesReady, AddressOf SensorAllFrameReady

                ' センサースタート！
                Try
                    Me.sensor.Start()
                Catch e1 As IOException
                    Me.sensor = Nothing
                End Try
            End If

            ' キネクト見つからなかったら
            If Nothing Is Me.sensor Then
                Me.statusBarText.Text = "No ready Kinect found!"
            End If
        End Sub


        ' 終了処理
        Private Sub WindowClosing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)
            If Nothing IsNot Me.sensor Then
                Me.sensor.Stop()
            End If
            Port.Close()
        End Sub

        ' AllFrameReadyイベント
        Private Sub SensorAllFrameReady(sender As Object, e As AllFramesReadyEventArgs)
            Dim skeletons(-1) As Skeleton

            ' ボーン取得
            Using skeletonFrame As SkeletonFrame = e.OpenSkeletonFrame()
                If skeletonFrame IsNot Nothing Then
                    skeletons = New Skeleton(skeletonFrame.SkeletonArrayLength - 1) {}
                    skeletonFrame.CopySkeletonDataTo(skeletons)
                End If
            End Using

            ' ボーン見つかったら腕の角度計算
            If skeletons.Length <> 0 Then
                For Each skel As Skeleton In skeletons
                    If skel.TrackingState = SkeletonTrackingState.Tracked Then
                        Me.CalcBoneAngle(skel)
                    End If
                Next skel
            End If

            ' 人物表示
            Using colorFrame As ColorImageFrame = e.OpenColorImageFrame()
                Using depthFrame As DepthImageFrame = e.OpenDepthImageFrame()
                    If (colorFrame IsNot Nothing) AndAlso (depthFrame IsNot Nothing) Then
                        Dim getBackgroundMask As Byte() = BackgroundMask(colorFrame, depthFrame)
                        Image.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgra32, Nothing, getBackgroundMask, colorFrame.Width * colorFrame.BytesPerPixel)
                    End If
                End Using
            End Using
        End Sub

        ' 背景を削除
        Private Function BackgroundMask(colorFrame As ColorImageFrame, depthFrame As DepthImageFrame) As Byte()
            Dim myColorStream As ColorImageStream = Me.sensor.ColorStream
            Dim myDepthStream As DepthImageStream = Me.sensor.DepthStream

            Dim colorPixel As Byte() = New Byte(colorFrame.PixelDataLength - 1) {}
            colorFrame.CopyPixelDataTo(colorPixel)

            Dim depthPixel As Short() = New Short(depthFrame.PixelDataLength - 1) {}
            depthFrame.CopyPixelDataTo(depthPixel)

            Dim colorPoint As ColorImagePoint() = New ColorImagePoint(depthFrame.PixelDataLength - 1) {}

#Disable Warning BC40000 ' 型またはメンバーが旧型式です
            sensor.MapDepthFrameToColorFrame(myDepthStream.Format, depthPixel, myColorStream.Format, colorPoint)
#Enable Warning BC40000 ' 型またはメンバーが旧型式です
            Dim opacityColor As Byte() = New Byte(colorPixel.Length - 1) {}

            For index As Integer = 0 To depthPixel.Length - 1
                Dim player As Integer = depthPixel(index) And DepthImageFrame.PlayerIndexBitmask
                Dim x As Integer = colorPoint(index).X
                Dim y As Integer = colorPoint(index).Y
                Dim colorIndex As Integer = ((y * depthFrame.Width) + x) * BgrPixel

                If player <> 0 Then
                    opacityColor(colorIndex) = colorPixel(colorIndex) 'Blue
                    opacityColor(colorIndex + 1) = colorPixel(colorIndex + 1) 'Green

                    opacityColor(colorIndex + 2) = colorPixel(colorIndex + 2) 'Red
                    opacityColor(colorIndex + 3) = &HFF 'Alpha
                End If
            Next
            Return opacityColor
        End Function

        ''' ボーン角度の計算
        Private Sub CalcBoneAngle(ByVal skeleton As Skeleton)
            ' point
            Dim R1 As SkeletonPoint = skeleton.Joints(JointType.ShoulderRight).Position
            Dim R2 As SkeletonPoint = skeleton.Joints(JointType.ElbowRight).Position
            Dim R3 As SkeletonPoint = skeleton.Joints(JointType.WristRight).Position
            Dim L1 As SkeletonPoint = skeleton.Joints(JointType.ShoulderLeft).Position
            Dim L2 As SkeletonPoint = skeleton.Joints(JointType.ElbowLeft).Position
            Dim L3 As SkeletonPoint = skeleton.Joints(JointType.WristLeft).Position
            Dim Rx, Ry As Double
            Dim Ra1, Ra2 As Integer
            Dim Lx, Ly As Double
            Dim La1, La2 As Integer

            ' 右肩→右ひじ
            Rx = R2.X - R1.X
            Ry = R2.Y - R1.Y
            If (Rx > 0) Then
                Ra1 = 90 + CInt(Math.Atan(Ry / Rx) * 180.0 / Math.PI)
            Else
                If (Ry > 0) Then
                    Ra1 = 180
                Else
                    Ra1 = 0
                End If
            End If

            ' 右ひじ→右手首
            Rx = R3.X - R2.X
            Ry = R3.Y - R2.Y
            If (Rx > 0) Then
                Ra2 = 90 + CInt(Math.Atan(Ry / Rx) * 180.0 / Math.PI) - Ra1
            Else
                Ra2 = 270 + CInt(Math.Atan(Ry / Rx) * 180.0 / Math.PI) - Ra1
            End If
            If (Ra2 < 0) Then
                Ra2 = 0
            End If

            ' 左肩→左ひじ
            Lx = L1.X - L2.X
            Ly = L1.Y - L2.Y
            If (Lx > 0) Then
                La1 = 90 - CInt(Math.Atan(Ly / Lx) * 180.0 / Math.PI)
            Else
                If (Ly > 0) Then
                    La1 = 0
                Else
                    La1 = 180
                End If
            End If

            ' 左ひじ→左手首
            Lx = L3.X - L2.X
            Ly = L3.Y - L2.Y
            If (Lx > 0) Then
                La2 = 270 - CInt(Math.Atan(Ly / Lx) * 180.0 / Math.PI) - La1
            Else
                La2 = 90 - CInt(Math.Atan(Ly / Lx) * 180.0 / Math.PI) - La1
            End If
            If (La2 < 0) Then
                La2 = 0
            End If

            Me.statusBarText.Text = "左" & La1.ToString & " " & La2.ToString & " 右" & Ra1.ToString & " " & Ra2.ToString
            Port.Write(Ra1.ToString & " " & Ra2.ToString & " " & La1.ToString & " " & La2.ToString & vbCrLf)
        End Sub
    End Class
End Namespace
